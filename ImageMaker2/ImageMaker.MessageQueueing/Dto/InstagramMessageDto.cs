﻿using System;

namespace ImageMaker.MessageQueueing.Dto
{
    public class InstagramMessageDto
    {
        public DateTime TransferTime { get; set; }

        public byte[] Data { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public string UserName { set; get; }

        public string Name { get; set; }

        public string FullName { get; set; }

        public string UrlAvatar { get; set; }

        public byte[] ProfilePictureData { get; set; }
    }
}
