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
    string AcctName = "WayMakers";
    private static string AcctId;
    private static string SessionToken;

    private static bool AccountFound = false;
    private static bool UsernameFound = false;
    private static bool LoggedIn = false;
    private static bool MembershipFound = false;
    private static bool CurrentSessionFound = true;
    private static bool GoodStanding = true;

    private static bool NextStep = false;

    private static bool AccountQueryFailed = false;
    private static bool UsernameQueryFailed = false;
    private static bool LoginFailed = false;
    private static bool MembershipQueryFailed = false;
    private static bool BadStanding = false;

    string url = "https://api.virbela.com/parse/";
    string appID = "zquCagFDNXC8ipCFPjRjV8xw7y4Jik";
    string key = "o3UfuztDWwYvE8GBgQRMewbd";


    private void Awake()
    {
        ParseInitializeBehaviour _script = new GameObject("ParseInitializeBehaviour").AddComponent<ParseInitializeBehaviour>();
        _script.applicationID = appID;
        _script.dotnetKey = key;
    }


    // Use this for initialization
    void Start () {

        if(AcctName.Equals("WayMakers"))
        {
            CurrentSessionFound = false;
            GoodStanding = false;
        }

        ParseClient.Initialize(new ParseClient.Configuration
        {
            ApplicationId = appID,
            WindowsKey = key,

            Server = url
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
        query.Limit(1).CountAsync().ContinueWith(t =>
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
            if(t.IsFaulted || t.IsCanceled)
            {
                AccountQueryFailed = true;
            }
            else
            {
                ParseObject ServerConfigObj = t.Result;
                if (!ServerConfigObj.ContainsKey("account"))
                {
                    AccountQueryFailed = true;
                }
                else
                {
                    ParseObject temp;
                    ServerConfigObj.TryGetValue<ParseObject>("account", out temp);
                    AcctId = temp.ObjectId;
                    AccountFound = true;
                    NextStep = true;
                }
            }
        });
    }

    public void FindMembership()
    {
        Debug.Log("Finding membership...");
        ParseQuery<ParseObject> query = ParseObject.GetQuery("Membership")
            .WhereEqualTo("account", ParseObject.CreateWithoutData("Account", AcctId))
            .WhereEqualTo("user", ParseUser.CreateWithoutData("_User", ParseUser.CurrentUser.ObjectId));
        query.Limit(1).CountAsync().ContinueWith(t =>
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

    public void GetCurrentSession()
    {
        Debug.Log("Getting Session token...");
        ParseSession.GetCurrentSessionAsync().ContinueWith(t =>
        {
            var result = t.Result;
            SessionToken = result.SessionToken;
            CurrentSessionFound = true;
            NextStep = true;
        });

    }

    public void CheckStanding()
    {
        Debug.Log("Checking standing...");
        IDictionary<string, object> dictionary = new Dictionary<string, object>
        {
            { "accountId", AcctId },
            { "sessionToken", SessionToken }
        };
        ParseCloud.CallFunctionAsync<IDictionary<string, object>>("isSubscribed", dictionary).ContinueWith(t =>
        {
            bool found = false;
            foreach (var item in t.Result)
            {
                if (item.Key.Equals("isSubscribed"))
                {
                    found = true;
                    NextStep = true;
                    GoodStanding = item.Value.ToString().Equals("True");
                    BadStanding = !GoodStanding;
                    NextStep = GoodStanding;
                    break;
                }
            }
            if(!found)
            {
                GoodStanding = false;
                BadStanding = !GoodStanding;
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

    private void ResetAllFlags()
    {
        //Manual multithreading controllers
        AccountFound = false;
        UsernameFound = false;
        LoggedIn = false;
        MembershipFound = false;
        NextStep = false;
        CurrentSessionFound = (AcctName.Equals("WayMakers")) ? false : true;
        GoodStanding = (AcctName.Equals("WayMakers")) ? false : true;

        //Error flags
        AccountQueryFailed = false;
        UsernameQueryFailed = false;
        LoginFailed = false;
        MembershipQueryFailed = false;
        BadStanding = false;
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
        else if (UsernameFound && LoggedIn && AccountFound && MembershipFound && NextStep && !CurrentSessionFound && !GoodStanding)
        {
            NextStep = false;
            GetCurrentSession();
        }
        else if (UsernameFound && LoggedIn && AccountFound && MembershipFound && NextStep && CurrentSessionFound && !GoodStanding)
        {
            NextStep = false;
            CheckStanding();
        }
        else if (UsernameFound && LoggedIn && AccountFound && MembershipFound && NextStep && CurrentSessionFound && GoodStanding)
        {
            ResetAllFlags();

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

        if (LoginFailed)
        {
            ResetAllFlags();
            Debug.Log("Invalid password");
            LoginFailed = false;
        }

        if (AccountQueryFailed)
        {
            ResetAllFlags();
            Debug.Log("Account verification failed");
            AccountQueryFailed = false;
            ParseUser.LogOutAsync();
        }

        if(MembershipQueryFailed)
        {
            ResetAllFlags();
            Debug.Log("Membership Query Failed");
            MembershipQueryFailed = false;
            ParseUser.LogOutAsync();
        }

        if (UsernameQueryFailed)
        {
            ResetAllFlags();
            Debug.Log("Invalid username");
            UsernameQueryFailed = false;
        }

        if (BadStanding)
        {
            ResetAllFlags();
            Debug.Log("Member is in bad standing");
            BadStanding = false;
            ParseUser.LogOutAsync();
        }

    }
}
