//---------------------------------------------------------------------------------
// Copyright 2010 Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License"); 
// You may not use this file except in compliance with the License. 
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 

// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, 
// INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR 
// CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, 
// MERCHANTABLITY OR NON-INFRINGEMENT. 

// See the Apache 2 License for the specific language governing 
// permissions and limitations under the License.
//---------------------------------------------------------------------------------

using Common.ACS.Management;
using System;
using System.Collections.Specialized;
using System.Data.Services.Client;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace ACS.Management
{
    /// <summary>
    /// This class obtains a SWT token and adds it to the HTTP authorize header 
    /// for every request to the management service.
    /// </summary>
    public sealed class ManagementServiceHelper
    {
        [DataContract]
        private class OAuth2TokenResponse
        {
            [DataMember]
            public string access_token;
        }

        private const string ACS_TOKEN_URI = "https://{0}.{1}";
        private const string ACS_TOKEN_REL_URL = "/v2/OAuth2-13";
        private const string MANAGEMENT_SERVICE_URI = "https://{0}.{1}/{2}";

        private string _cachedSwtToken;
        private AccessControlConfiguration _acsConfig;

        public ManagementServiceHelper(AccessControlConfiguration acsConfig)
        {
            _acsConfig = acsConfig;
        }

        /// <summary>
        /// Creates and returns a ManagementService object. This is the only 'interface' used by other classes.
        /// </summary>
        /// <returns>An instance of the ManagementService.</returns>
        public ManagementService CreateManagementServiceClient()
        {
            string managementServiceEndpoint;
            ManagementService managementService;

            managementServiceEndpoint = string.Format(CultureInfo.InvariantCulture,
                                                      MANAGEMENT_SERVICE_URI, 
                                                      _acsConfig.ServiceNamespace, 
                                                      _acsConfig.AcsHostUrl, 
                                                      _acsConfig.AcsManagementServicesRelativeUrl);
            
            managementService = new ManagementService(new Uri(managementServiceEndpoint));
            managementService.SendingRequest += AddTokenWithWritePermission;
            
            return managementService;
        }

        internal void AddTokenWithWritePermission(HttpWebRequest httpRequest)
        {
            httpRequest.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + GetTokenFromACS());
        }

        /// <summary>
        /// Event handler for getting a token from ACS, adding the SWT token to the HTTP 'Authorization' header.
        /// The SWT token is cached so that we don't need to obtain a token on every request.
        /// </summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="args">Event arguments.</param>
        private void AddTokenWithWritePermission(object sender, SendingRequestEventArgs args)
        {
            var httpRequest = (HttpWebRequest)args.Request;

            if (_cachedSwtToken == null)
                _cachedSwtToken = GetTokenFromACS();

            httpRequest.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + _cachedSwtToken);
        }

        /// <summary>
        /// Obtains a SWT token from ACSv2. 
        /// </summary>
        /// <returns>A token  from ACS.</returns>
        private string GetTokenFromACS()
        {
            string accessToken;
            byte[] byteResponse;
            OAuth2TokenResponse tokenResponse;
            var client = new WebClient();
            var values = new NameValueCollection();
            var serializer = new DataContractJsonSerializer(typeof(OAuth2TokenResponse));
            
            client.BaseAddress = string.Format(CultureInfo.CurrentCulture,
                                               ACS_TOKEN_URI, 
                                               _acsConfig.ServiceNamespace,
                                               _acsConfig.AcsHostUrl);

            values.Add("grant_type", "client_credentials");
            values.Add("client_id", _acsConfig.ManagementServiceName);
            values.Add("client_secret", _acsConfig.ManagementServiceKey);
            values.Add("scope", client.BaseAddress + _acsConfig.AcsManagementServicesRelativeUrl);

            byteResponse = client.UploadValues(ACS_TOKEN_REL_URL, "POST", values);

            using(var response = new MemoryStream(byteResponse))
            {
                tokenResponse = (OAuth2TokenResponse)serializer.ReadObject(response);
                accessToken = tokenResponse.access_token;
            }

            return accessToken;
        }
    }
}

