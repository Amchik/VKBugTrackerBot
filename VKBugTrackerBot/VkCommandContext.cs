using System;
using VkNet;
using VkNet.Model;
using VkNet.Model.RequestParams;

namespace VKBugTrackerBot
{
    public sealed class VkCommandContext : ICommandContext<MessagesSendParams>
    {
        private Func<MessagesSendParams, Object> _func_send = null;

        public UserPreferences User { get; }
        public String Command { get; }
        public String[] Arguments { get; }

        public Message Message { get; }

        public VkCommandContext(UserPreferences user, Message message, String command, params String[] args)
        {
            User = user;
            Message = message;
            Command = command;
            Arguments = args;
        }
        public VkCommandContext InitWithVkApi(VkApi api)
        {
            if (_func_send != null) throw new InvalidOperationException("Already defined");
            _func_send = (e) =>
            {
                api.Messages.Send(e);
                return null;
            };
            return this;
        }
        public VkCommandContext InitWithFunc(Func<MessagesSendParams, Object> func)
        {
            if (_func_send != null) throw new InvalidOperationException("Already defined");
            _func_send = func;
            return this;
        }

        void ICommandContext.Send(Object @params) => Send(@params as MessagesSendParams);
        public void Send(MessagesSendParams @params)
        {
            if (_func_send == null) throw new NullReferenceException("Send function cannot be null.");
            _func_send(@params);
        }

        public void Reply(MessagesSendParams @params)
        {
            @params.PeerId = Message?.PeerId;
            Send(@params);
        }
    }
}
