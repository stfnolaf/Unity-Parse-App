using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Parse;
using System.Threading.Tasks;
using UnityEngine.UI;

public class ParseController : MonoBehaviour {

    public GameObject LogInMenu;
    public GameObject SignUpMenu;
    public GameObject LoggedInMenu;
    public GameObject WelcomeText;
    public InputField usernameLogin;
    public InputField passwordLogin;
    public InputField usernameSignup;
    public InputField passwordSignup;
    public InputField RetypePassword;
    string AcctName;
    private static ParseObject AcctObj;

    private static bool AccountFound = false;
    private static bool UsernameFound = false;
    private static bool LoggedIn = false;
    private static bool MembershipFound = false;

    private static bool NextStep = false;

    private static bool AccountQueryFailed = false;
    private static bool UsernameQueryFailed = false;
    private static bool LoginFailed = false;
    private static bool MembershipQueryFailed = false;

	// Use this for initialization
	void Start () {

        AcctName = "eXpTest";

        ParseInitializeBehaviour _script = new GameObject("ParseInitializeBehaviour").AddComponent<ParseInitializeBehaviour>();
        _script.applicationID = "zquCagFDNXC8ipCFPjRjV8xw7y4Jik";
        _script.dotnetKey = "o3UfuztDWwYvE8GBgQRMewbd";

        ParseClient.Initialize(new ParseClient.Configuration
        {
            ApplicationId = "zquCagFDNXC8ipCFPjRjV8xw7y4Jik",
            WindowsKey = "o3UfuztDWwYvE8GBgQRMewbd",

            Server = "http://staging.api.virbela.com/parse/"
        });

        if(ParseUser.CurrentUser != null)
        {
            ParseUser.LogOutAsync();
        }

        SignUpMenu.SetActive(false);
        LoggedInMenu.SetActive(false);

        passwordLogin.contentType = InputField.ContentType.Password;
        passwordSignup.contentType = InputField.ContentType.Password;
        RetypePassword.contentType = InputField.ContentType.Password;

    }

    public void ToggleSignUpMenu()
    {
        LogInMenu.SetActive(!LogInMenu.activeSelf);
        SignUpMenu.SetActive(!SignUpMenu.activeSelf);
    }

    public void BeginLogIn()
    {
        Debug.Log("Querying username...");
        ParseQuery<ParseUser> query = ParseUser.Query.WhereEqualTo("username", usernameLogin.text);
        query.CountAsync().ContinueWith(t =>
        {
            if (t.Result == 0)
            {
                UsernameQueryFailed = true;
            }
            else
            {
                UsernameFound = true;
                NextStep = true;
            }
        });
    }

    public void LogIn()
    {
        Debug.Log("Checking password...");
        if(usernameLogin.text.Equals("") || passwordLogin.text.Equals(""))
        {
            return;
        }
        ParseUser.LogInAsync(usernameLogin.text, passwordLogin.text).ContinueWith(t =>
        {
            if (t.IsFaulted || t.IsCanceled)
            {
                LoginFailed = true;
            }
            else
            {
                LoggedIn = true;
                NextStep = true;
            }
        });
    }

    public void FindAccount()
    {
        Debug.Log("Finding account...");
        ParseQuery<ParseObject> query = ParseObject.GetQuery("ServerConfig").WhereEqualTo("name", AcctName);
        query.FirstAsync().ContinueWith(t =>
        {
            ParseObject ServerConfigObj = t.Result;
            if(!ServerConfigObj.ContainsKey("account"))
            {
                AccountQueryFailed = true;
            }
            else
            {
                ServerConfigObj.TryGetValue<ParseObject>("account", out AcctObj);
                AccountFound = true;
                NextStep = true;
            }
        });
    }

    public void FindMembership()
    {
        Debug.Log("Finding membership...");
        ParseQuery<ParseObject> query = ParseObject.GetQuery("Membership").WhereEqualTo("account", AcctObj).WhereEqualTo("user", ParseUser.CurrentUser);
        query.CountAsync().ContinueWith(t =>
        {
            if(t.Result == 0)
            {
                MembershipQueryFailed = true;
            }
            else
            {
                MembershipFound = true;
                NextStep = true;
            }
        });
    }

    public void CreateUser()
    {
        if(usernameSignup.text.Equals("") || passwordSignup.text.Equals(""))
        {
            return;
        }
        if(passwordSignup.text.Equals(RetypePassword.text))
        {
            var user = new ParseUser()
            {
                Username = usernameSignup.text,
                Password = passwordSignup.text
            };
            Task signUpTask = user.SignUpAsync();
            Debug.Log("Creating user: " + usernameSignup.text);
            usernameSignup.text = "";
            passwordSignup.text = "";
            RetypePassword.text = "";
            SignUpMenu.SetActive(false);
            LoggedInMenu.SetActive(true);
            WelcomeText.GetComponent<Text>().text = usernameSignup.text;
        }
        else
        {
            Debug.Log("Passwords do not match");
        }
        
    }

    public void LogOut()
    {
        ParseUser.LogOutAsync();
        LoggedInMenu.SetActive(false);
        LogInMenu.SetActive(true);
        LoggedIn = false;
    }
	
	// Update is called once per frame
	void Update () {

        if (UsernameFound && !LoggedIn && !AccountFound && !MembershipFound && NextStep)
        {
            NextStep = false;
            LogIn();
        }
        else if (UsernameFound && LoggedIn && !AccountFound && !MembershipFound && NextStep)
        {
            NextStep = false;
            FindAccount();
        }
        else if (UsernameFound && LoggedIn && AccountFound && !MembershipFound && NextStep)
        {
            NextStep = false;
            FindMembership();
        }
        else if (UsernameFound && LoggedIn && AccountFound && MembershipFound && NextStep)
        {
            NextStep = false;
            AccountFound = false;
            UsernameFound = false;
            LoggedIn = false;
            MembershipFound = false;

            Debug.Log("Logging in...");
            usernameLogin.text = "";
            passwordLogin.text = "";
            LogInMenu.SetActive(false);
            LoggedInMenu.SetActive(true);
            string name = "";
            if (ParseUser.CurrentUser.TryGetValue<string>("displayname", out name))
            {
                WelcomeText.GetComponent<Text>().text = "Welcome, " + name + "!";
                LoggedIn = true;
            }
        }

        if(AccountQueryFailed)
        {
            Debug.Log("Account verification failed");
            AccountQueryFailed = false;
        }

        if(LoginFailed)
        {
            Debug.Log("Invalid password");
            LoginFailed = false;
        }

        if(MembershipQueryFailed)
        {
            Debug.Log("Membership Query Failed");
            MembershipQueryFailed = false;
        }

        if(UsernameQueryFailed)
        {
            Debug.Log("Invalid username");
            UsernameQueryFailed = false;
        }

    }
}
