using KalshiBotData.Data.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using SmokehouseDTOs;
using SmokehouseDTOs.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TradingStrategies.Strategies;
using TradingStrategies.Strategies.Strats;
using TradingStrategies.Trading.Helpers;
using SmokehouseBot.Services.Interfaces;
using TradingStrategies.Trading.Overseer;
using TradingStrategies.Classification;
using static SmokehouseInterfaces.Enums.StrategyEnums;

namespace TradingStrategies
{
    public class ActionDecision
    {
        public ActionType Type { get; set; }
        public int Price { get; set; } = 0; // For limit orders
        public int Qty { get; set; } = 1; // For limit orders
        public DateTime? Expiration { get; set; } = null; // Optional expiration for limits
        public string Memo { get; set; }
    }

}