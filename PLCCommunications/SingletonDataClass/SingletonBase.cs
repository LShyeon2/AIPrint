﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLCCommunications.SingletonDataClass
{
    public class SingletonBase<T> where T : class, new()
    {
        private static readonly Lazy<T> instance = new Lazy<T>(() => new T());
        public static T Instance
        {
            get
            {
                return instance.Value;
            }
        }
    }
}
