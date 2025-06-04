using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;

namespace MonkeyScheduler.WorkerService.Tests.Extensions
{
    public static class HttpMessageHandlerExtensions
    {
        public static void SetupSendAsync(this Mock<HttpMessageHandler> mock, HttpResponseMessage response)
        {
            mock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
        }

        public static void SetupSendAsync(this Mock<HttpMessageHandler> mock, Exception exception)
        {
            mock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(exception);
        }

        public static void SetupSequenceSendAsync(this Mock<HttpMessageHandler> mock, params HttpResponseMessage[] responses)
        {
            var sequence = mock.Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>());

            foreach (var response in responses)
            {
                sequence.ReturnsAsync(response);
            }
        }

        public static void VerifySendAsync(
            this Mock<HttpMessageHandler> mock,
            string expectedUrl,
            HttpMethod expectedMethod,
            Times times)
        {
            mock.Protected().Verify(
                "SendAsync",
                times,
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.ToString() == expectedUrl &&
                    req.Method == expectedMethod),
                ItExpr.IsAny<CancellationToken>());
        }
    }
} 