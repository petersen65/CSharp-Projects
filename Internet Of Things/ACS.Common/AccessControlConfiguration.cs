//---------------------------------------------------------------------------------
// Copyright 2013 Microsoft Corporation
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Management
{
    public sealed class AccessControlConfiguration
    {
        private const string ACS_HOST_URL = "accesscontrol.windows.net";
        private const string ACS_MANAGEMENT_REL_URL = "v2/mgmt/service/";
        private const string ACS_SERVICE_NAMESPACE = "{0}-sb";

        public string ServiceNamespace { get; private set; }
        public string ManagementServiceName { get; private set; }
        public string ManagementServiceKey { get; private set; }
        public string AcsHostUrl { get; private set; }
        public string AcsManagementServicesRelativeUrl { get; private set; }

        public AccessControlConfiguration(string serviceNamespace, string managementServiceName, string managementServiceKey)
        {
            AcsHostUrl = ACS_HOST_URL;
            AcsManagementServicesRelativeUrl = ACS_MANAGEMENT_REL_URL;

            ServiceNamespace = string.Format(ACS_SERVICE_NAMESPACE, serviceNamespace);
            ManagementServiceName = managementServiceName;
            ManagementServiceKey = managementServiceKey;
        }
    }
}
