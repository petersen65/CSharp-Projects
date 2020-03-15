using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Messaging;

namespace DPAG.PoC.KISS.Core
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class LargeMessage
    {
        public const int MIN_MSMQ_CAPACITY = 1;
        public const int MAX_MSMQ_CAPACITY = (4 * (1024 * 1024)) - (10 * 1024);

        private const int APP_SPECIFIC_BODY_NULL = 1024;
        private const int GUID_BYTE_LENGTH = 16;
        private const string SEPARATOR_MESSAGE_EXTENSION = "\t\t";
        private const string FORMAT_TO_STRING_SKIPPED = ".. ";
        private const string FORMAT_TO_STRING_HEX_NUMBER = "{0:X2} ";

        private int _msmqCapacity;
        private string _correlation, 
                       _outputHeader;
        private byte[] _dataBuffer, 
                       _messageExtension;
        private Guid _conversation;

        public int Length
        {
            get
            {
                return _dataBuffer != null ? _dataBuffer.Length : 0;
            }
        }

        public byte[] DataBuffer
        {
            get
            {
                return _dataBuffer;
            }
        }

        public byte[] MessageExtension
        {
            get
            {
                return _messageExtension;
            }
        }

        public Guid Conversation
        {
            get
            {
                return _conversation;
            }
        }

        public string Correlation
        {
            get
            {
                return _correlation;
            }
        }

        public string OutputHeader
        {
            get
            {
                return _outputHeader;
            }

            set
            {
                _outputHeader = string.IsNullOrEmpty(value) ? string.Empty : value;
            }
        }

        public static byte[] EncodeMessageExtension(string[] values)
        {
            return Encoding.UTF8.GetBytes(string.Join(SEPARATOR_MESSAGE_EXTENSION, values));
        }

        public static string[] DecodeMessageExtension(byte[] extension)
        {
            string[] values = null;

            if (extension != null && extension.Length > 0)
            {
                values = Encoding.UTF8.
                    GetString(extension).Split(new[] { SEPARATOR_MESSAGE_EXTENSION }, StringSplitOptions.None);
            }

            return values;
        }

        public static byte[] ToMessageExtension(Guid conversation)
        {
            return ToMessageExtension(conversation, null);
        }

        public static byte[] ToMessageExtension(Guid conversation, byte[] messageExtension)
        {
            byte[] extension,
                   conversationBytes = conversation.ToByteArray();

            if (messageExtension != null && messageExtension.Length > 0)
            {
                extension = new byte[conversationBytes.Length + messageExtension.Length];

                Array.Copy(conversationBytes, extension, conversationBytes.Length);
                Array.Copy(messageExtension, 0, extension, conversationBytes.Length, messageExtension.Length);
            }
            else
                extension = conversationBytes;

            return extension;
        }

        public static void FromMessageExtension(byte[] extension, out Guid conversation)
        {
            var conversationBytes = new byte[GUID_BYTE_LENGTH];

            Array.Copy(extension, conversationBytes, conversationBytes.Length);
            conversation = new Guid(conversationBytes);
        }

        public static void FromMessageExtension(byte[] extension, out Guid conversation, out byte[] messageExtension)
        {
            FromMessageExtension(extension, out conversation);

            if (extension.Length > GUID_BYTE_LENGTH)
            {
                messageExtension = new byte[extension.Length - GUID_BYTE_LENGTH];
                Array.Copy(extension, GUID_BYTE_LENGTH, messageExtension, 0, messageExtension.Length);
            }
            else
                messageExtension = null;
        }

        public static Guid[] GetAvailableConversations(MessageQueue queue)
        {
            Guid currentConversation;
            var conversations = new List<Guid>();

            using (var messageCursor = queue.GetMessageEnumerator2())
            {
                while (messageCursor.MoveNext())
                {
                    using (var currentMessage = messageCursor.Current)
                    {
                        if (currentMessage.IsLastInTransaction)
                        {
                            FromMessageExtension(currentMessage.Extension, out currentConversation);
                            conversations.Add(currentConversation);
                        }
                    }
                }
            }

            return conversations.ToArray();
        }

        public static string[] GetMessagesOfConversation(MessageQueue queue, Guid conversation)
        {
            Guid currentConversation;
            var convMessages = new List<string>();

            using (var messageCursor = queue.GetMessageEnumerator2())
            {
                while (messageCursor.MoveNext())
                {
                    using (var currentMessage = messageCursor.Current)
                    {
                        FromMessageExtension(currentMessage.Extension, out currentConversation);

                        if (currentConversation == conversation)
                        {
                            convMessages.Add(currentMessage.Id);
                            
                            if (currentMessage.IsLastInTransaction)
                                break;
                        }
                    }
                }
            }

            return convMessages.ToArray();
        }

        public static bool IsCorrelationAvailable(MessageQueue queue, string correlationId)
        {
            var available = false;

            using (var messageCursor = queue.GetMessageEnumerator2())
            {
                while (messageCursor.MoveNext())
                {
                    using (var currentMessage = messageCursor.Current)
                    {
                        if (currentMessage.IsLastInTransaction && currentMessage.CorrelationId == correlationId)
                        {
                            available = true;
                            break;
                        }
                    }
                }
            }

            return available;
        }

        public static Guid[] GetConversationsOfCorrelation(MessageQueue queue, string correlationId)
        {
            Guid currentConversation;
            var conversations = new List<Guid>();

            using (var messageCursor = queue.GetMessageEnumerator2())
            {
                while (messageCursor.MoveNext())
                {
                    using (var currentMessage = messageCursor.Current)
                    {
                        if (currentMessage.CorrelationId == correlationId)
                        {
                            FromMessageExtension(currentMessage.Extension, out currentConversation);

                            if (!conversations.Contains(currentConversation))
                                conversations.Add(currentConversation);
                        }
                    }
                }
            }

            return conversations.ToArray();
        }

        public LargeMessage(byte[] dataBuffer, byte[] messageExtension, 
                            int msmqCapacity, string correlation, string outputHeader)
        {
            _dataBuffer = dataBuffer;
            _messageExtension = messageExtension;

            _msmqCapacity =
                msmqCapacity >= MIN_MSMQ_CAPACITY && msmqCapacity <= MAX_MSMQ_CAPACITY ?
                    msmqCapacity : MAX_MSMQ_CAPACITY;

            _correlation = string.IsNullOrEmpty(correlation) ? string.Empty : correlation;
            _outputHeader = string.IsNullOrEmpty(outputHeader) ? string.Empty : outputHeader;
            _conversation = Guid.Empty;
        }

        public LargeMessage(int msmqCapacity, string outputHeader) :
            this(null, null, msmqCapacity, string.Empty, outputHeader)
        {
        }

        public string ToString(int start)
        {
            var maximumIndex = Length;
            var output = new StringBuilder(_outputHeader);

            if (maximumIndex > 0)
            {
                if (start >= maximumIndex)
                    start = 0;
                else
                    start = Math.Abs(start);

                if (maximumIndex - start <= 25)
                {
                    for (var i = start; i < maximumIndex; i++)
                        output.AppendFormat(FORMAT_TO_STRING_HEX_NUMBER, _dataBuffer[i]);
                }
                else
                {
                    for (var i = start; i < start + 15; i++)
                        output.AppendFormat(FORMAT_TO_STRING_HEX_NUMBER, _dataBuffer[i]);
                    
                    output.Append(FORMAT_TO_STRING_SKIPPED);

                    for (var i = maximumIndex - 10; i < maximumIndex; i++)
                        output.AppendFormat(FORMAT_TO_STRING_HEX_NUMBER, _dataBuffer[i]);
                }

                output.Remove(output.Length - 1, 1);
            }

            return output.ToString();
        }

        public override string ToString()
        {
            return ToString(0);
        }

        public void EncodeExtension(string[] values)
        {
            _messageExtension = LargeMessage.EncodeMessageExtension(values);
        }

        public string[] DecodeExtension()
        {
            return LargeMessage.DecodeMessageExtension(_messageExtension);
        }

        public void Send(MessageQueue queue, MessageQueueTransaction transaction, TimeSpan? ttrq, TimeSpan? ttbr)
        {
            bool lastInTransaction;
            int bytesCount,
                currentIndex, 
                maximumIndex;
            byte[] conversationExtension, 
                   lastInTransactionExtension;

            _conversation = Guid.NewGuid();
            conversationExtension = ToMessageExtension(_conversation);
            lastInTransactionExtension = ToMessageExtension(_conversation, _messageExtension);

            if (Length == 0)
            {
                using (var msg = new Message { AppSpecific = APP_SPECIFIC_BODY_NULL })
                {
                    SetMessageCorrelation(msg, _correlation);
                    SetMessageTimeouts(msg, ttrq, ttbr);
                    msg.Extension = lastInTransactionExtension;

                    if (transaction != null)
                        queue.Send(msg, transaction);
                    else
                        queue.Send(msg, MessageQueueTransactionType.Automatic);
                }
            }
            else
            {
                currentIndex = 0;
                maximumIndex = Length;

                do
                {
                    bytesCount = (int)Math.Min(maximumIndex - currentIndex, _msmqCapacity);
                    lastInTransaction = currentIndex + bytesCount == maximumIndex;

                    using (var dataStream = new MemoryStream(_dataBuffer, currentIndex, bytesCount))
                    using (var msg = new Message { BodyStream = dataStream })
                    {
                        SetMessageCorrelation(msg, _correlation);
                        SetMessageTimeouts(msg, ttrq, ttbr);
                        msg.Extension = lastInTransaction ? lastInTransactionExtension : conversationExtension;

                        if (transaction != null)
                            queue.Send(msg, transaction);
                        else
                            queue.Send(msg, MessageQueueTransactionType.Automatic);
                    }

                    currentIndex += bytesCount;
                } while (!lastInTransaction);
            }
        }

        public void Receive(MessageQueue queue, MessageQueueTransaction transaction, 
                            string[] largeMessage, int timeoutReceive)
        {
            int currentIndex;
            Message msg = null;
            var dataBuffer = new byte[0];
            var timeout = TimeSpan.FromSeconds(timeoutReceive);

            _conversation = Guid.Empty;
            _dataBuffer = _messageExtension = null;

            for (var i = 0; i < largeMessage.Length; i++)
            {
                try
                {
                    if (transaction != null)
                        msg = queue.ReceiveById(largeMessage[i], timeout, transaction);
                    else
                        msg = queue.ReceiveById(largeMessage[i], timeout, MessageQueueTransactionType.Automatic);
                    
                    if (msg.AppSpecific != APP_SPECIFIC_BODY_NULL)
                    {
                        currentIndex = dataBuffer.Length;

                        Array.Resize(ref dataBuffer, currentIndex + (int)msg.BodyStream.Length);
                        msg.BodyStream.Read(dataBuffer, currentIndex, (int)msg.BodyStream.Length);
                    }

                    if (i == largeMessage.Length - 1)
                    {
                        _correlation = string.IsNullOrEmpty(msg.CorrelationId) ? string.Empty : msg.CorrelationId;

                        if (msg.Extension != null && msg.Extension.Length > 0)
                            FromMessageExtension(msg.Extension, out _conversation, out _messageExtension);
                    }
                }
                finally
                {
                    if (msg != null)
                        msg.Dispose();
                }
            }

            _dataBuffer = dataBuffer;
        }

        private static void SetMessageCorrelation(Message msg, string correlation)
        {
            if (!string.IsNullOrEmpty(correlation))
                msg.CorrelationId = correlation;
        }

        private static void SetMessageTimeouts(Message msg, TimeSpan? timeToReachQueue, TimeSpan? timeToBeReceived)
        {
            if (timeToReachQueue != null)
                msg.TimeToReachQueue = timeToReachQueue.Value;

            if (timeToBeReceived != null)
                msg.TimeToBeReceived = timeToBeReceived.Value;
        }
    }
}
