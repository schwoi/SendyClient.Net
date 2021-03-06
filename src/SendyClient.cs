﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Sendy.Client.Model;

[assembly: InternalsVisibleTo("Sendy.Client.Tests")]

namespace Sendy.Client
{
	public class SendyClient : IDisposable
	{
		private readonly string _apiKey;
		private readonly HttpClient _httpClient;

		public SendyClient(Uri baseUri, string apiKey) : this(baseUri, apiKey, null)
		{
		}

		/// <summary>
		/// This one should only be used for unit tests to support injecting of the httpClient
		/// </summary>
		internal SendyClient(Uri baseUri, string apiKey, HttpClient httpClient = null)
		{
			_apiKey = apiKey;
			_httpClient = httpClient ?? new HttpClient();
			_httpClient.BaseAddress = baseUri;
		}

		public Task<SendyResponse> Subscribe(string emailAddress, string name, string listId)
		{
			return Subscribe(emailAddress, name, listId, null);
		}

		/// <param name="customFields">For custom fields, use Sendy fieldname as key value.</param>
		public async Task<SendyResponse> Subscribe(string emailAddress, string name, string listId, Dictionary<string, string> customFields)
		{
			var postData = GetPostData();
			postData.Add(new KeyValuePair<string, string>("email", emailAddress));
			postData.Add(new KeyValuePair<string, string>("name", name));
			postData.Add(new KeyValuePair<string, string>("list", listId));

			AppendCustomFields(postData, customFields);
			var subscribeData = new FormUrlEncodedContent(postData);

			var result = await _httpClient.PostAsync("subscribe", subscribeData);

			return await SendyResponseHelper.HandleResponse(result, SendyResponseHelper.SendyActions.Subscribe);
		}

		public async Task<SendyResponse> Unsubscribe(string emailAddress, string listId)
		{
			var postData = GetPostData();
			postData.Add(new KeyValuePair<string, string>("email", emailAddress));
			postData.Add(new KeyValuePair<string, string>("list", listId));

			var subscribeData = new FormUrlEncodedContent(postData);

			var result = await _httpClient.PostAsync("unsubscribe", subscribeData);

			return await SendyResponseHelper.HandleResponse(result, SendyResponseHelper.SendyActions.Unsubscribe);
		}

		public async Task<SendyResponse> DeleteSubscriber(string emailAddress, string listId)
		{
			var postData = GetPostData();
			postData.Add(new KeyValuePair<string, string>("email", emailAddress));
			postData.Add(new KeyValuePair<string, string>("list_id", listId));

			var subscribeData = new FormUrlEncodedContent(postData);

			var result = await _httpClient.PostAsync("api/subscribers/delete.php", subscribeData);

			return await SendyResponseHelper.HandleResponse(result, SendyResponseHelper.SendyActions.DeleteSubscriber);
		}

		public async Task<SendyResponse> SubscriptionStatus(string emailAddress, string listId)
		{
			var postData = GetPostData();
			postData.Add(new KeyValuePair<string, string>("email", emailAddress));
			postData.Add(new KeyValuePair<string, string>("list_id", listId));

			var subscribeData = new FormUrlEncodedContent(postData);

			var result = await _httpClient.PostAsync("api/subscribers/subscription-status.php", subscribeData);

			return await SendyResponseHelper.HandleResponse(result, SendyResponseHelper.SendyActions.SubscriptionStatus);
		}

		public async Task<SendyResponse> ActiveSubscriberCount(string listId)
		{
			var postData = GetPostData();
			postData.Add(new KeyValuePair<string, string>("list_id", listId));

			var subscribeData = new FormUrlEncodedContent(postData);

			var result = await _httpClient.PostAsync("api/subscribers/active-subscriber-count.php", subscribeData);
			//test return value
			return await SendyResponseHelper.HandleResponse(result, SendyResponseHelper.SendyActions.ActiveSubscriberCount);
		}

		/// <param name="campaign"></param>
		/// <param name="send">True to send the campaign as well. In that case <paramref name="listIds" /> is also required.</param>
		/// <param name="listIds">Lists to send to campaign to. Only required if <paramref name="send"/> is true.</param>
		public async Task<SendyResponse> CreateCampaign(Campaign campaign, bool send, List<string> listIds)
		{
			if (send && listIds == null)
				throw new ArgumentNullException(nameof(listIds), "Please provide one or more list ids to send this campaign to.");

			var postData = GetPostData();
			postData.Add(new KeyValuePair<string, string>("from_name", campaign.FromName));
			postData.Add(new KeyValuePair<string, string>("from_email", campaign.FromEmail));
			postData.Add(new KeyValuePair<string, string>("reply_to", campaign.ReplyTo));
			postData.Add(new KeyValuePair<string, string>("title", campaign.Title));
			postData.Add(new KeyValuePair<string, string>("subject", campaign.Subject));
			postData.Add(new KeyValuePair<string, string>("plain_text", campaign.PlainText));
			postData.Add(new KeyValuePair<string, string>("html_text", campaign.HtmlText));
			postData.Add(new KeyValuePair<string, string>("brand_id", campaign.BrandId.ToString()));
			postData.Add(new KeyValuePair<string, string>("query_string", campaign.Querystring));

			if (send)
			{
				postData.Add(new KeyValuePair<string, string>("send_campaign", "1"));
				postData.Add(new KeyValuePair<string, string>("list_ids", string.Join(",", listIds)));
			}
			var subscribeData = new FormUrlEncodedContent(postData);

			var result = await _httpClient.PostAsync("api/campaigns/create.php", subscribeData);

			return await SendyResponseHelper.HandleResponse(result, SendyResponseHelper.SendyActions.CreateCampaign);
		}

		/// <summary>
		/// Creates a new mailing list.
		/// </summary>
		/// <param name="mailingList"></param>
		/// <returns>The id of the list or the error message.</returns>
		public async Task<SendyResponse> CreateList(MailingList mailingList)
		{
			var postData = GetPostData();
			postData.Add(new KeyValuePair<string, string>("list_name", mailingList.Name));
			postData.Add(new KeyValuePair<string, string>("brand_id", mailingList.BrandId.ToString()));

			if(mailingList.CustomFields.Any())
			{
				postData.Add(new KeyValuePair<string, string>("custom_fields", string.Join(",", mailingList.CustomFields.Select(c => c.Name))));
				postData.Add(new KeyValuePair<string, string>("field_types", string.Join(",", mailingList.CustomFields.Select(c => c.DataType.ToString()))));
			}
			var subscribeData = new FormUrlEncodedContent(postData);

			var result = await _httpClient.PostAsync("api/lists/create.php", subscribeData);

			return await SendyResponseHelper.HandleResponse(result, SendyResponseHelper.SendyActions.CreateList);
		}

		private List<KeyValuePair<string, string>> GetPostData()
		{
			return new List<KeyValuePair<string, string>>
			{
				new KeyValuePair<string, string>("api_key", _apiKey),
				new KeyValuePair<string, string>("boolean", "true") //otherwise it could return a whole html page
			};
		}

		private static void AppendCustomFields(List<KeyValuePair<string, string>> postData, Dictionary<string, string> customFields)
		{
			if (customFields != null && customFields.Any())
			{
				foreach (var customField in customFields)
				{
					postData.Add(new KeyValuePair<string, string>(customField.Key, customField.Value));
				}
			}
		}

		public void Dispose()
		{
			_httpClient?.Dispose();
		}
	}
}
