﻿using System.Threading.Tasks;

namespace DataContract;

public interface IServiceAsync
{
    Task<string> CallAsync(string s);
}