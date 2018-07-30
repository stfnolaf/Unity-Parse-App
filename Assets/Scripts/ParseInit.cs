using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Parse;
using System.Threading.Tasks;
using UnityEngine.UI;

public class ParseInit : MonoBehaviour {

    public GameObject LogInMenu;
    public GameObject SignUpMenu;
    public GameObject LoggedInMenu;
    public GameObject WelcomeText;
    public InputField usernameLogin;
    public InputField passwordLogin;
    public InputField usernameSignup;
    public InputField passwordSignup;
    public InputField RetypePassword;
    bool LoggedIn = false;

	// Use this for initialization
	void Start () {

        ParseInitializeBehaviour _script = new GameObject("ParseInitializeBehaviour").AddComponent<ParseInitializeBehaviour>();
        _script.applicationID = "myAppId";
        _script.dotnetKey = "master";

        ParseClient.Initialize(new ParseClient.Configuration
        {
            ApplicationId = "myAppId",
            WindowsKey = "master",

            Server = "http://localhost:1337/parse/"
        });

        if(ParseUser.CurrentUser != null)
        {
            SignUpMenu.SetActive(false);
            LogInMenu.SetActive(false);
            string name = "";
            if(ParseUser.CurrentUser.TryGetValue<string>("username", out name))
            {
                WelcomeText.GetComponent<Text>().text = name;
            }
            LoggedIn = true;
        }
        else
        {
            SignUpMenu.SetActive(false);
            LoggedInMenu.SetActive(false);
        }

        passwordLogin.contentType = InputField.ContentType.Password;
        passwordSignup.contentType = InputField.ContentType.Password;
        RetypePassword.contentType = InputField.ContentType.Password;

    }

    public void ToggleSignUpMenu()
    {
        LogInMenu.SetActive(!LogInMenu.activeSelf);
        SignUpMenu.SetActive(!SignUpMenu.activeSelf);
    }

    public void LogIn()
    {
        if(usernameLogin.text.Equals("") || passwordLogin.text.Equals(""))
        {
            return;
        }
        ParseUser.LogInAsync(usernameLogin.text, passwordLogin.text).ContinueWith(t =>
        {
            if (t.IsFaulted || t.IsCanceled)
            {
                Debug.Log("Login failed");
            }
            else
            {
                ParseUser currUser;
                Debug.Log("Login succeeded");
                ParseUser.Query.WhereEqualTo("username", usernameLogin.text).FirstAsync().ContinueWith(v =>
                {
                    currUser = v.Result;
                });
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

        if (ParseUser.CurrentUser != null && !LoggedIn)
        {
            Debug.Log("Logging in...");
            usernameLogin.text = "";
            passwordLogin.text = "";
            LogInMenu.SetActive(false);
            LoggedInMenu.SetActive(true);
            string name = "";
            if (ParseUser.CurrentUser.TryGetValue<string>("username", out name))
            {
                WelcomeText.GetComponent<Text>().text = name;
                LoggedIn = true;
            }
        }

    }
}
