﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.Services
{
    public interface IPXLogWriterService
    {
        void Info(string s);
        void Exception(Exception e);
        void Error(string s);
        void Debug(string s);
    }
}
