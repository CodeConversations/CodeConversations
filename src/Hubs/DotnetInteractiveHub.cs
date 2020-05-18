using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using CodeConversations.Workers;

namespace CodeConversations.Hubs
{
    public class DotnetInteractiveHub : Hub
    {
        public async Task StreamEventsForCommand(string token)
        {
            var caller = Clients.Caller;
            var channel = ContentSubjectHelper.GetOrCreateChannel(token);
            channel.Subscribe(async content =>
            {
                await caller.SendAsync("outputReceived", content.Value);
            },
            () =>
            {
                ContentSubjectHelper.DeleteExpired();
            });
            
#pragma warning disable CS4014,CS1998
            DotNetInteractiveProcessRunner.Instance.ExecuteEnvelope(token);
#pragma warning restore CS4014,CS1998

            await caller.SendAsync("outputEnded");
        }
    }
}