using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SmokehouseBot.Services.Interfaces
{
    public interface IScopeManagerService : IDisposable
    {
        void InitializeScope();
        void ResetAll();
        IServiceScope Scope { get; }

    }
}