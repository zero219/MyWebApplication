using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace Common.HttpRequest
{
    public interface IApiClient
    {
        /// <summary>
        /// get请求
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="url"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        Task<TResponse> GetAsync<TResponse>(string url, TimeSpan? timeout = null);

        /// <summary>
        ///  post请求
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="url"></param>
        /// <param name="request"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest request, TimeSpan? timeout = null);

    }
}
