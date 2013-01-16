fbsdk
=====

Facebook SDK for .NET

Port from the Facebook PHP sdk version 3.2.2
This port tries to mimic the PHP SDK as literal as possible, so all good examples for the PHP SDK should be very easy to port to .NET.

The Facebook Platform is a set of APIs that make your app more social.

This repository contains an open source .NET SDK that allows you to access Facebook Platform from your .NET web app. 
Except as otherwise noted, the Facebook NET SDK is licensed under the Apache Licence, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0.html).

Usage

The examples are a good place to start. 

using facebook;
FacebookConfig fbConfig = new FacebookConfig() 
{ 
  appId = "160789593985275", 
	apiSecret = "b4cb207732c89f34c94f8d259df07b8c" 
};
//for local debugging and custom persistent store - force domain - should allow cross-domain authentication using a single FB app
fbConfig.appDomain = "fbAsp.hylas.be";
fb = new Facebook(fbConfig);

// Get User ID
	user = fb.getUser();
	
To make API calls:

	if (!string.IsNullOrEmpty(user))
	{
		try
		{
			...
		
