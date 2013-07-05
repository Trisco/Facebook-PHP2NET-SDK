using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using facebook;
using System.Collections.Specialized;

public partial class _Default : System.Web.UI.Page
{
    string user;
    Facebook fb;
    FacebookResponse fbr;
    public String[] permissionsUser = { "email", "publish_actions", "user_about_me", "user_actions.music", "user_actions.news", "user_actions.video", "user_activities", "user_birthday", "user_education_history", "user_events", "user_games_activity", "user_groups", "user_hometown", "user_interests", "user_likes", "user_location", "user_notes", "user_photos", "user_questions", "user_relationship_details", "user_relationships", "user_religion_politics", "user_status", "user_subscriptions", "user_videos", "user_website", "user_work_history" };
    public String[] permissionsFriends = { "friends_about_me", "friends_actions.music", "friends_actions.news", "friends_actions.video", "friends_birthday", "friends_education_history", "friends_games_activity", "friends_groups", "friends_interests", "friends_likes", "friends_notes", "friends_photos", "friends_relationship_details", "friends_relationships", "friends_status", "friends_subscriptions", "friends_website", "friends_work_history" };
    public String[] permissionsExtended = { "ads_management", "create_note", "export_stream", "manage_friendlists", "manage_notifications", "offline_access", "photo_upload", "publish_stream", "read_friendlists", "read_mailbox", "read_page_mailboxes", "read_stream", "rsvp_event", "sms", "status_update", "video_upload", "xmpp_login" };

    protected void Page_Load(object sender, EventArgs e)
    {
        FacebookConfig fbConfig = new FacebookConfig() 
        { 
            appId = "160789593985275", 
            apiSecret = "<secret>" 
        };
        //for local debugging and custom persistent store - force domain - should allow cross-domain authentication using a single FB app
		//copy your authtoken from your published app in your local app.
//        fbConfig.appDomain = "fbAsp.hylas.be";
/*
note: for quick and easy debugging on localhost:
1. create facebook app for local debugging:
	- App Domains: localhost
	- Sandbox Mode: enabled (only admins can use app)
	- Website with Facebook Login: http://localhost:<VS debugger or IIS express port>/Example/
2. it works! (after a minute or two...)
3. but you should put appId & apiSecrit in your web.config, using a web.config transformation to switch between your debug & release application
*/
        fb = new Facebook(fbConfig);
        this.initPermissions();
        pnlPermissions.Visible = false;
        if (IsPostBack)
        {
            if (txtAccessToken.Text.Length > 0) fb.setAccessToken(txtAccessToken.Text);
            try
            {
                //fb.getUser should only fail when graph is unavailable or respons is corrupt
                user = fb.getUser();
            }
            catch (Exception ex)
            {
                iPanel.Visible = false;
                lblStatus.Text = "Error communicating with graph servers";
                fbURL.NavigateUrl = fb.getLoginUrl();
                ViewState["user"] = null;
                return;
            }        
        }
        else
        {
            try
            {
                user = fb.getUser();
            }
            catch (Exception ex) {
                iPanel.Visible = false;
                lblStatus.Text = "Error communicating with graph servers";
                fbURL.NavigateUrl = fb.getLoginUrl();
                ViewState["user"] = null;
                return;
            }
            if (!string.IsNullOrEmpty(user))
            {
                try
                {
                    iPanel.Visible = true;
                    txtAccessToken.Text = fb.getAccessToken();
                    fbr = fb._graph("/me");
                    txtRequests.Text = "/me";
                    txtData.Text = fbr.ToString();
                    //txtUserObect.Text = fbr.ToString();
                    iPicture.ImageUrl = "https://graph.facebook.com/" + fbr["id"].ToString() + "/picture";
                    iProfile.Text = fbr["name"].ToString();// fbr["name"].value.ToString();
                    lblStatus.Text = "Connected";
                    btnConnect1.Text = "Disconnect";
                    btnConnect1.CssClass = "btnDisconnect";
                    fbr = fb._graph("/me/albums");
                    fbURL.NavigateUrl = fb.getLogoutUrl();
                    if (fbr.ContainsKey("data"))
                    {
                        for (var i = 0; i < fbr["data"].Count; i++)
                            AlbumList.Items.Add(new ListItem(fbr["data"][i.ToString()]["name"].ToString(), fbr["data"][i.ToString()]["id"].ToString()));
                        //or different way:
                        /*foreach (KeyValuePair<String, FacebookResponse> entry in fbr["data"])
                        {
                            
                            AlbumList.Items.Add(new ListItem(entry.Value["name"].ToString()));
                        }*/
                    }
                    while (fbr.ContainsKey("paging") && fbr["paging"].ContainsKey("next"))
                    {
                        fbr = fb._graph(fbr["paging"]["next"].ToString());
                        if (fbr.ContainsKey("data"))
                            for (var i = 0; i < fbr["data"].Count; i++)
                                AlbumList.Items.Add(new ListItem(fbr["data"][i.ToString()]["name"].ToString(), fbr["data"][i.ToString()]["id"].ToString()));                        
                    }
                    Session.Remove("fb_connect_attempt") ;

                }
                catch (FacebookApiException ex)
                {
                    //todo: log error
                    //probably session expired: we still have the persistent data: user_id and access_token, but the token is no longer valid
                    //best is just to clear the session/cookie OR auto reconnect
                    
                    txtAccessToken.Text = "";
                    iPanel.Visible = false;
                    user = null;
                    if (Session["fb_connect_attempt"] == null)
                    {
                        Session["fb_connect_attempt"] = 1;
                        connectToFB();
                    }
                }
            }
            else
            {
                iPanel.Visible = false;
                lblStatus.Text = "Not Connected";
                fbURL.NavigateUrl = fb.getLoginUrl();
                ViewState["user"] = null;
            }
        }
    }

    private void clearError()
    {
        lblError.Text = "";
    }

    protected void initPermissions()
    {
        int row = 0, col = 0, counter = 0;
        TableRow tr = new TableRow();
        Label lbl;
        CheckBox cb;
        foreach (String Uperm in permissionsUser)
        {
            if (counter++ % 3 == 0)
            {
                tr = new TableRow();
                perms_user.Rows.Add(tr);
            }
            TableCell tc = new TableCell();
            cb = new CheckBox();
            cb.ID = "fbUPerm_" + counter;
            cb.Checked = false;
            cb.Text = Uperm;
            cb.Attributes.Add("class", "permcheck");
            tc.Controls.Add(cb);
            tr.Controls.Add(tc);
        }
        if (counter > 0) perms_user.Rows.Add(tr);
        counter = 0; tr = new TableRow();
        foreach (String Fperm in permissionsFriends)
        {
            if (counter++ % 3 == 0)
            {
                tr = new TableRow();
                perms_friends.Rows.Add(tr);
            }
            TableCell tc = new TableCell();
            cb = new CheckBox();
            cb.ID = "fbFPerm_" + counter;
            cb.Checked = false;
            cb.Text = Fperm;
            cb.Attributes.Add("class", "permcheck");
            tc.Controls.Add(cb);
            tr.Controls.Add(tc);
        }
        if (counter > 0) perms_friends.Rows.Add(tr);
        counter = 0; tr = new TableRow();
        foreach (String Eperm in permissionsExtended)
        {
            if (counter++ % 3 == 0)
            {
                tr = new TableRow();
                perms_extended.Rows.Add(tr);
            }
            TableCell tc = new TableCell();
            cb = new CheckBox();
            cb.ID = "fbEPerm_" + counter;
            cb.Checked = false;
            cb.Text = Eperm;
            cb.Attributes.Add("class", "permcheck");
            tc.Controls.Add(cb);
            tr.Controls.Add(tc);
        }
        if (counter > 0) perms_extended.Rows.Add(tr);
    }

    protected void btnPush_Click(object sender, EventArgs e)
    {
        try
        {
            string filePath = HttpRuntime.CodegenDir + "/" + fuPush.FileName;

            lblError.Text = filePath;

            if (fuPush.HasFile)
            {
                fuPush.SaveAs(filePath);

                try
                {
                    fb.setFileUploadSupport(true);

                    if (fb.useFileUploadSupport())
                    {
                        Dictionary<string, string> uploadParam = new Dictionary<string, string>();
                        uploadParam.Add("source", filePath);
                        uploadParam.Add("message", "Photo upload test");

                        fb._graph(txtPushLocation.Text, "POST", uploadParam);
                    }
                    else
                    {
                        lblError.Text = "Facebook does not support fileupload";
                    }
                }
                catch (Exception ex)
                {
                    lblError.Text = ex.Message;
                    user = null;
                }
            }

            lblError.Text = "FileUpload Succesfull";

        }
        catch (Exception ex)
        {
            lblError.Text = ex.Message;
        }
        
    }

    protected void btnConnect_Click(object sender, EventArgs e)
    {
       
         connectToFB();
    }

    private void connectToFB()
    {
        string scope = loopControls(pnlPermissions); //"user_photos,email,publish_stream";
        CheckBox c;
        NameValueCollection qs = Request.QueryString;
        if (scope.Length == 0) scope = "user_photos,email,publish_stream";
        else scope = scope.Substring(1);
        Dictionary<string, string> pars = new Dictionary<string, string>();
        pars.Add("scope", scope);

        Response.Redirect(fb.getLoginUrl(pars));
    }

    private string loopControls(Control cc)
    {
        String val = "";
        if (cc is CheckBox && (cc as CheckBox).Checked)
            val = ("," + (cc as CheckBox).Text);
        else if (cc.Controls.Count > 0)
            foreach (Control c in cc.Controls)
                val += loopControls(c);         
        return val;   
    }

    protected void btnCall_Click(object sender, EventArgs e)
    {
        fbr = null; //Make sure previous response is cleared           
        this.clearError();
        if (!string.IsNullOrEmpty(user))        //User logged in
        {
            try
            {
                fbr = fb._graph(txtCall.Text); //Making the actual call


                if (fbr != null)    //valid response
                {
                    txtRequests.Text = txtRequests.Text + "\n" + txtCall.Text;
                    txtData.Text = fbr.ToString();
                }
                else
                {
                    txtRequests.Text = txtRequests.Text + "\n" + txtCall.Text;
                    txtData.Text = "No Response";
                }
            }
            catch (FacebookApiException ex)
            {
                user = null;
                lblError.Text = ex.Message;
            }
        }
        else //Not yet logged in
        {
            txtRequests.Text = "Not logged in";
            txtData.Text = "Not logged in";
        }
    }

    protected void AlbumList_SelectedIndexChanged(object sender, EventArgs e)
    {
        txtPushLocation.Text = "/" + AlbumList.SelectedValue + "/photos";
    }

    protected void btnConnect1_Click(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(txtAccessToken.Text)) //User logged in
        {
            Dictionary<string, string> paras = new Dictionary<string, string>();
            //paras.Add("next", "http://fbasp.hylas.be/?logout=1");
            fb.destroySession();
            //logout of facebook => switch following two lines
            //Response.Redirect(fb.getLogoutUrl(paras));
            Response.Redirect(HttpContext.Current.Request.ApplicationPath);
        }
        else //User not logged in
        {
            pnlPermissions.Visible = true;
        }
    }

    protected void btnCancel_Click(object sender, EventArgs e)
    {
        pnlPermissions.Visible = false;
    }


}

