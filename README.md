fbsdk
=====

###Facebook SDK for .NET

Port from the [Facebook PHP sdk version 3.2.2](https://github.com/facebook/facebook-php-sdk)
This port tries to mimic the [PHP SDK](https://github.com/facebook/facebook-php-sdk) as literal as possible, so all good examples for the PHP SDK should be very easy to port to .NET.

The Facebook Platform is a set of APIs that make your app more social.

This repository contains an open source .NET SDK that allows you to access Facebook Platform from your .NET web app. 
Except as otherwise noted, the Facebook .NET SDK is licensed under the Apache Licence, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0.html).

Demo
=====

http://fbasp.hylas.be/

Note: for quick and easy debugging on localhost
===============================================

1. create a facebook app for local debugging only:
	- App Domains: localhost
	- Sandbox Mode: enabled (only admins can use app)
	- Website with Facebook Login: http://localhost:<VS debugger or IIS express port>/Example/
2. it works! (after a minute or two...)
3. but you really should put appId & apiSecret in your web.config, using a web.config transformation to switch between your debug & release application

Usage
=====

The examples are a good place to start. 

	using facebook;
	FacebookConfig fbConfig = new FacebookConfig() 
	{ 
		appId = "<Your App ID>", 
		apiSecret = "<Your App Secret>" 
	};
	//for local debugging and custom persistent store - force domain - should allow cross-domain authentication using a single FB app
	fbConfig.appDomain = "<Your App Domain>";
	fb = new Facebook(fbConfig);

	// Get User ID
	user = fb.getUser();
	
To make API calls:

	if (!string.IsNullOrEmpty(user))
	{
		try
		{          
                    fbr = fb._graph("/me");      
                    iPicture.ImageUrl = "https://graph.facebook.com/" + fbr["id"].ToString() + "/picture";
                    iProfile.Text = fbr["name"].ToString();// fbr["name"].value.ToString();
                    
                    fbr = fb._graph("/me/albums");
                    if (fbr.ContainsKey("data"))
                    {
                        for (var i = 0; i < fbr["data"].Count; i++)
                            AlbumList.Items.Add(new ListItem(fbr["data"][i.ToString()]["name"].ToString(), fbr["data"][i.ToString()]["id"].ToString()));
			...
		
