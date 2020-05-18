using System;
using System.Reactive.Subjects;
using Microsoft.DotNet.Interactive;

namespace CodeConversations.Workers
{
    public class ContentSubject : ISubject<FormattedValue>
    {
        public string Token { get; }

        private readonly ReplaySubject<FormattedValue> _channel = new ReplaySubject<FormattedValue>();

        public ContentSubject(string token)
        {
            Token = token;
            CreationTime = DateTime.UtcNow;
        }

        public DateTime CreationTime { get;  }

        public void OnCompleted()
        {
            _channel.OnCompleted();
        }

        public void OnError(Exception error)
        {
            _channel.OnError(error);
        }

        public void OnNext(FormattedValue value)
        {
            _channel.OnNext(value);
        }

        public IDisposable Subscribe(IObserver<FormattedValue> observer)
        {
            return _channel.Subscribe(observer);
        }
    }
}