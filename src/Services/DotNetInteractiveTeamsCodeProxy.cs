using System;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Server;

namespace CodeConversations.Services
{
    public class DotNetInteractiveCodeConversationsProxy
    {
        public event CodeReceivedEventHandler CodeReceived; 


        internal ReplaySubject<KernelEvent> ReceiveCode(string token,
            string code, 
            string language = "csharp")
        {
            var stream = new ReplaySubject<KernelEvent>();

            CodeReceived?.Invoke(this, new CodeReceivedEventArgs
            {
                CommandToken = token,
                KernelEventStream = stream
            });

            if (CodeReceived == null)
            {
                stream.OnError(new InvalidOperationException("No code handler registered."));
            }

            return stream;
        }
    }

    public delegate Task CodeReceivedEventHandler(object sender, CodeReceivedEventArgs args);
}