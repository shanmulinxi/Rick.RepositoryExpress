﻿using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Agent
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public int Status { get; set; }
        public long Adduser { get; set; }
        public DateTime Addtime { get; set; }
        public long Lastuser { get; set; }
        public DateTime Lasttime { get; set; }
        public string Contact { get; set; }
        public string Mobile { get; set; }
        public string Address { get; set; }
    }
}
