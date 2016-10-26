using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Transport;

namespace NServiceBus.FakeTransport
{
    public class MessagePump : IPushMessages
    {
        private Func<MessageContext, Task> onMessage;
        private Func<ErrorContext, Task<ErrorHandleResult>> onError;
        private CriticalError criticalError;
        private PushSettings pushSettings;
        private SemaphoreSlim concurrencyLimiter;
        private bool started = false;

        public string inputQueue;
        private PushRuntimeSettings pushRuntimeSettings;

        public Task Init(
            Func<MessageContext, Task> onMessage,
            Func<ErrorContext, Task<ErrorHandleResult>> onError,
            CriticalError criticalError,
            PushSettings settings)
        {
            pushSettings = settings;
            this.criticalError = criticalError;
            this.onError = onError;
            this.onMessage = onMessage;

            inputQueue = settings.InputQueue;

            return Task.CompletedTask;
        }

        public void Start(PushRuntimeSettings limitations)
        {
            pushRuntimeSettings = limitations;
            concurrencyLimiter = new SemaphoreSlim(limitations.MaxConcurrency);
            started = true;
        }

        public async Task Stop()
        {
            started = false;
            while (concurrencyLimiter.CurrentCount != pushRuntimeSettings.MaxConcurrency)
            {
                await Task.Yield();
            }
        }

        public async Task Push(
            byte[] messageBody,
            string messageId = null,
            Dictionary<string, string> headers = null,
            int retryAttempt = 0)
        {
            if (!started)
            {
                return;
            }

            try
            {
                await concurrencyLimiter.WaitAsync();
                await PushInternal(messageBody, messageId, headers, retryAttempt);
            }
            finally
            {
                concurrencyLimiter.Release();
            }
        }

        private async Task PushInternal(byte[] messageBody, string messageId, Dictionary<string, string> headers, int retryAttempt)
        {
            var transportTransaction = new TransportTransaction();
            InMemoryTransaction inMemoryTransaction = null;
            if (this.pushSettings.RequiredTransactionMode == TransportTransactionMode.SendsAtomicWithReceive)
            {
                transportTransaction.Set(inMemoryTransaction = new InMemoryTransaction());
            }

            messageId = messageId ?? Guid.NewGuid().ToString();
            headers = headers ?? new Dictionary<string, string>();
            var contextBag = new ContextBag();
            var receiveCancellationTokenSource = new CancellationTokenSource();
            var messageContext = new MessageContext(
                messageId,
                headers,
                messageBody,
                transportTransaction,
                receiveCancellationTokenSource,
                contextBag);

            try
            {
                await onMessage(messageContext);

                if (receiveCancellationTokenSource.IsCancellationRequested)
                {
                    await PushInternal(messageBody, messageId, headers, retryAttempt);
                }

                inMemoryTransaction?.Complete();
            }
            catch (Exception e)
            {
                await HandleError(messageBody, retryAttempt, e, headers, messageId);
            }
        }

        private async Task HandleError(
            byte[] messageBody,
            int retryAttempt,
            Exception exception,
            Dictionary<string, string> headers,
            string messageId)
        {
            var transportTransaction = new TransportTransaction();
            InMemoryTransaction errorHandlingTransaction = null;
            if (this.pushSettings.RequiredTransactionMode == TransportTransactionMode.SendsAtomicWithReceive)
            {
                transportTransaction.Set(errorHandlingTransaction = new InMemoryTransaction());
            }

            try
            {
                var immediateProcessingFailures = ++retryAttempt;
                var handleResult = await onError(new ErrorContext(
                    exception,
                    headers,
                    messageId,
                    messageBody,
                    transportTransaction,
                    immediateProcessingFailures));

                if (handleResult == ErrorHandleResult.RetryRequired)
                {
                    await PushInternal(messageBody, messageId, headers, immediateProcessingFailures);
                }
                else
                {
                    errorHandlingTransaction?.Complete();
                }
            }
            catch (Exception e)
            {
                criticalError.Raise("Exception while handling pipeline error.", e);
                await HandleError(messageBody, ++retryAttempt, exception, headers, messageId);
            }
        }
    }
}