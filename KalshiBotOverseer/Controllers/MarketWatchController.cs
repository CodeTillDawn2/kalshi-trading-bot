using KalshiBotData.Data.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace KalshiBotOverseer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MarketWatchController : ControllerBase
    {
        private readonly IKalshiBotContext _context;

        public MarketWatchController(IKalshiBotContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return PhysicalFile(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", "marketwatch.html"), "text/html");
        }

        [HttpGet("data")]
        public async Task<IActionResult> GetMarketWatchData()
        {
            var marketWatches = await _context.GetMarketWatches();
            var markets = await _context.GetMarkets();

            var marketWatchData = marketWatches.Select(mw => new
            {
                mw.market_ticker,
                mw.BrainLock,
                mw.InterestScore,
                mw.InterestScoreDate,
                mw.LastWatched,
                mw.AverageWebsocketEventsPerMinute,
                Market = markets.FirstOrDefault(m => m.market_ticker == mw.market_ticker) == null ? null : new
                {
                    markets.FirstOrDefault(m => m.market_ticker == mw.market_ticker)?.title,
                    markets.FirstOrDefault(m => m.market_ticker == mw.market_ticker)?.subtitle,
                    markets.FirstOrDefault(m => m.market_ticker == mw.market_ticker)?.market_type,
                    markets.FirstOrDefault(m => m.market_ticker == mw.market_ticker)?.status,
                    markets.FirstOrDefault(m => m.market_ticker == mw.market_ticker)?.open_time,
                    markets.FirstOrDefault(m => m.market_ticker == mw.market_ticker)?.close_time,
                    markets.FirstOrDefault(m => m.market_ticker == mw.market_ticker)?.yes_bid,
                    markets.FirstOrDefault(m => m.market_ticker == mw.market_ticker)?.yes_ask,
                    markets.FirstOrDefault(m => m.market_ticker == mw.market_ticker)?.no_bid,
                    markets.FirstOrDefault(m => m.market_ticker == mw.market_ticker)?.no_ask,
                    markets.FirstOrDefault(m => m.market_ticker == mw.market_ticker)?.last_price,
                    markets.FirstOrDefault(m => m.market_ticker == mw.market_ticker)?.volume,
                    markets.FirstOrDefault(m => m.market_ticker == mw.market_ticker)?.liquidity,
                    markets.FirstOrDefault(m => m.market_ticker == mw.market_ticker)?.open_interest
                }
            }).ToList();

            return Ok(marketWatchData);
        }
    }
}