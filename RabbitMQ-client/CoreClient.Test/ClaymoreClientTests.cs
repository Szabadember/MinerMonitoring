namespace CoreClient.Test
{
    using System;
    using Xunit;
    using CoreClient;

    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            // Given
            var host = "localhost";
            var port = 1234;
            var retryCount = 3;
            var cc = new ClaymoreClient(host, port, retryCount);

            // When
            var obs = cc.requestStats();
            
            // Then
        }
    }
}
