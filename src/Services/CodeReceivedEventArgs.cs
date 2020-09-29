using System;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Server;

namespace CodeConversations.Services
{
    public class CodeReceivedEventArgs : EventArgs
    {
        public IKernelCommandEnvelope KernelCommandEnvelope { get; set; }
        public IObserver<KernelEvent> KernelEventStream { get; set; }
        public string CommandToken { get; set; }
    }
}