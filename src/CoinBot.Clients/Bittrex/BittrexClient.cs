﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CoinBot.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CoinBot.Clients.Bittrex
{
	public class BittrexClient : IMarketClient
	{
		/// <summary>
		/// The <see cref="CurrencyManager"/>.
		/// </summary>
		private readonly CurrencyManager _currencyManager;

		/// <summary>
		/// The Exchange name.
		/// </summary>
		public string Name => "Bittrex";

		/// <summary>
		/// The <see cref="Uri"/> of the CoinMarketCap endpoint.
		/// </summary>
		private readonly Uri _endpoint = new Uri("https://bittrex.com/api/v1.1/public/", UriKind.Absolute);

		/// <summary>
		/// The <see cref="HttpClient"/>.
		/// </summary>
		private readonly HttpClient _httpClient;

		/// <summary>
		/// The <see cref="ILogger"/>.
		/// </summary>
		private readonly ILogger _logger;

		/// <summary>
		/// The <see cref="JsonSerializerSettings"/>.
		/// </summary>
		private readonly JsonSerializerSettings _serializerSettings;

		public BittrexClient(ILogger logger, CurrencyManager currencyManager)
		{
			this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
		    this._currencyManager = currencyManager ?? throw new ArgumentNullException(nameof(currencyManager));
		    this._httpClient = new HttpClient
			{
				BaseAddress = this._endpoint
			};

		    this._serializerSettings = new JsonSerializerSettings
			{
				Error = (sender, args) =>
				{
					Exception ex = args.ErrorContext.Error.GetBaseException();
					this._logger.LogError(new EventId(args.ErrorContext.Error.HResult), ex, ex.Message);
				}
			};
		}

		/// <inheritdoc/>
		public async Task<IReadOnlyCollection<MarketSummaryDto>> Get()
		{
			try
			{
				List<BittrexMarketSummaryDto> summaries = await this.GetMarketSummaries();
				return summaries.Select(m => new MarketSummaryDto
				{
					BaseCurrrency = this._currencyManager.Get(m.MarketName.Substring(0, m.MarketName.IndexOf('-'))),
					MarketCurrency = this._currencyManager.Get(m.MarketName.Substring(m.MarketName.IndexOf('-') + 1)),
					Market = "Bittrex",
					Volume = m.BaseVolume,
					Last = m.Last,
					LastUpdated = m.TimeStamp
				}).ToList();
			}
			catch (Exception e)
			{
				this._logger.LogError(new EventId(e.HResult), e, e.Message);
				throw;
			}
		}

		/// <summary>
		/// Get the market summaries.
		/// </summary>
		/// <returns></returns>
		private async Task<List<BittrexMarketSummaryDto>> GetMarketSummaries()
		{
			using (HttpResponseMessage response = await this._httpClient.GetAsync(new Uri("getmarketsummaries", UriKind.Relative)))
			{
				BittrexMarketSummariesDto summaries = JsonConvert.DeserializeObject<BittrexMarketSummariesDto>(await response.Content.ReadAsStringAsync(), this._serializerSettings);
				return summaries.Result;
			}
		}
	}
}
