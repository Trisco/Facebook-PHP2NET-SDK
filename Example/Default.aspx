<%@ Page Title="Facebook ASP SDK" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeFile="Default.aspx.cs" CodeBehind="Default.aspx.cs" Inherits="_Default" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
    <script runat="server">
       
    </script>
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
    <h2>
        Facebook ASP SDK
    </h2>
    <div class="clear row">
        <asp:Panel style="width:300px; float:left" ID="iStatus" runat="server">
            <asp:Button ID="btnConnect1" runat="server" Text="Connect" 
                onclick="btnConnect1_Click" />&nbsp;        
            <asp:Label ID="lblStatus" runat="server" Text="Not connected" Width="200px"></asp:Label>
        </asp:Panel>        
        <asp:Panel style="width:590px; float:right" ID="iPanel" runat="server">
            <asp:Image ID="iPicture" runat="server" />
            <asp:Label ID="iProfile" runat="server" Text="Label"></asp:Label>
        </asp:Panel>
   </div>
    <div class="clear row">
        <asp:Label ID="Label1" runat="server" Text="Access Token" ></asp:Label>&nbsp;
        <asp:TextBox ID="txtAccessToken" runat="server" style="width:720px;"></asp:TextBox>
        <asp:HyperLink ID="fbURL" runat="server" Visible="false" />
    </div>
    <div class="clear row">
        <asp:Label ID="Label2" runat="server" Text="Graph Path"></asp:Label>
        <asp:TextBox ID="txtCall" runat="server" Width="200px"></asp:TextBox>&nbsp;
        <asp:Button ID="btnCall" runat="server" Text="Call" OnClick="btnCall_Click" />
    </div>
    <div class="clear row">
        <h5>Upload Image to Selected Album</h5> 
        <span class="rowsection"><asp:DropDownList ID="AlbumList" runat="server" AutoPostBack="true" EnableViewState="true"
            OnSelectedIndexChanged="AlbumList_SelectedIndexChanged" Width="169px">
        </asp:DropDownList><label>Facebook Album</label></span>
        <span class="rowsection"><asp:TextBox ID="txtPushLocation" runat="server" Width="200px"></asp:TextBox><label>Graph Path</label>&nbsp;</span>
        <span class="rowsection"><asp:FileUpload ID="fuPush" runat="server" />
        <asp:Button ID="btnPush" runat="server" Text="Upload" OnClick="btnPush_Click" /><label>Select Image</label></span>
    </div>
    <div class="clear row">
        <asp:TextBox CssClass="colLeft" ID="txtRequests" runat="server" Rows="10" TextMode="MultiLine" style="width:230px;max-width:230px;float:left;min-height:230px;"></asp:TextBox>
        <asp:TextBox CssClass="colRight" ID="txtData" runat="server" Rows="10" TextMode="MultiLine" style=" float: left; height: 100%;margin-left: 20px;overflow: auto;width: 650px; max-width:650px;min-height:230px;"></asp:TextBox>
        <br />
    </div>
    <div class="clear row">
        <asp:Label CssClass="error" runat="server" ID="lblError"></asp:Label>
    </div>
    <asp:Panel runat="server" ID="pnlPermissions">
    <div class="generic_dialog pop_dialog" id="permissions-dialog">
        <div class="generic_dialog_popup" 
            style="top: 91px; width: 577px; z-index: 999;">
            <div class="pop_container_advanced">
                <div id="pop_content" class="pop_content " tabindex="0" role="alertdialog">
                <h2 class="dialog_title  secure" id="title_dialog_0"><span>Select Permissions</span></h2>
                <div class="dialog_content"><div class="dialog_summary ">
                <ul class="uiList uiListHorizontal clearfix">
    <li class="uiListItem  uiListHorizontalItemBorder uiListHorizontalItem"><a id="tab_user" class="uiPillButton   uiPillButtonSelected" rel="MainContent_perms_user">User Data Permissions</a></li>
    <li class="pls uiListItem  uiListHorizontalItemBorder uiListHorizontalItem"><a id="tab_friends" class="uiPillButton" rel="MainContent_perms_friends">Friends Data Permissions</a></li>
    <li class="pls uiListItem  uiListHorizontalItemBorder uiListHorizontalItem"><a id="tab_extended" class="uiPillButton" rel="MainContent_perms_extended">Extended Permissions</a></li>
                </ul>
                </div>
                <div class="dialog_body" id="permsForm">
    <asp:Table CellPadding="0" CellSpacing="0" runat="server" ID="perms_user" CssClass="uiGrid"></asp:Table>
    <asp:Table CellPadding="0" CellSpacing="0" runat="server" ID="perms_friends" CssClass="uiGrid hidden_elem"></asp:Table>
    <asp:Table CellPadding="0" CellSpacing="0" runat="server" ID="perms_extended" CssClass="uiGrid hidden_elem"></asp:Table>
    </div><div class="dialog_buttons clearfix ">
    <div class="dialog_buttons_msg">Basic Permissions already included by default</div>
    <div><label class="uiButton uiButtonLarge uiButtonConfirm">
            <asp:Button ID="btnConnect" runat="server" Text="Connect" OnClick="btnConnect_Click" />&nbsp;        
            <!--input type="button" name="refresh_token" value="Get Access Token"></label-->
        <label class="uiButton uiButtonLarge ">
        <asp:Button ID="btnCancel" runat="server" Text="Cancel" 
            onclick="btnCancel_Click" /></label></div></div><div class="dialog_footer hidden_elem"></div></div><div class="dialog_loading">Laden...</div></div></div></div></div>
        </label>
    </asp:Panel>
    <script type="text/javascript">
        $(function () {
            $("#permissions-dialog .uiPillButton").live("click", function () {
                if ($(this).hasClass("uiPillButtonSelected")) return false;
                $(".uiPillButton").removeClass("uiPillButtonSelected");
                $(this).addClass("uiPillButtonSelected");
                var ref = $(this).attr("rel");
                $(".uiGrid").addClass("hidden_elem");
                $("#" + ref).removeClass("hidden_elem");
                return false;
            });
            /*$("#permissions-dialog input:button").click(function () {
                if ($(this).attr("name") == "cancel") {
                    $("#permissions-dialog").hide();
                }
                else if ($(this).attr("name") == "refresh_token") {
                    btnConnect_Click();
                }
                return false;
            });*/
            
        });
    </script>
    <asp:HiddenField id="hfImgUrl" runat="server"/>
</asp:Content>
