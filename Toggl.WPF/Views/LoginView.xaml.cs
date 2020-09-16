using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using ReactiveUI;
using Toggl.Core.UI.ViewModels;
using Toggl.Shared;
using Toggl.Shared.Extensions;

namespace Toggl.WPF.Views
{
    public partial class LoginView : ReactiveUserControl<LoginViewModel>
    {
        private readonly CompositeDisposable disposeBag = new CompositeDisposable();
        public LoginView(LoginViewModel loginViewModel)
        {
            ViewModel = loginViewModel;
            InitializeComponent();
        }

        private void LoginView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Email
            var emailTextChanged = Observable.FromEventPattern<TextChangedEventHandler, TextChangedEventArgs>(
                h => emailTextBox.TextChanged += h,
                h => emailTextBox.TextChanged -= h
            ).Select(x => ((TextBox)x.Sender).Text);

            emailTextChanged
                .Select(Email.From)
                .Subscribe(ViewModel.Email.Accept)
                .DisposeWith(disposeBag);

            ViewModel.Email
                .Select(x => x.ToString())
                .Subscribe(email => emailTextBox.Text = email)
                .DisposeWith(disposeBag);;

            // Password
            var passwordChanged = Observable.FromEventPattern<RoutedEventHandler, RoutedEventArgs>(
                h => passwordBox.PasswordChanged += h,
                h => passwordBox.PasswordChanged -= h
            ).Select(x => ((PasswordBox)x.Sender).Password);

            ViewModel.Password
                .Select(x => x.ToString())
                .Take(1)
                .Subscribe(passwordBox.TextObserver())
                .DisposeWith(disposeBag);

            passwordChanged
                .DistinctUntilChanged()
                .Select(Password.From)
                .Subscribe(ViewModel.Password.Accept)
                .DisposeWith(disposeBag);

            // Login
            this.loginButton.Command = ViewModel.Login.ToCommand();
            Disposable.Create(this.loginButton, button => button.Command = null)
                .DisposeWith(disposeBag);

            ViewModel.Login.Executing
                .Subscribe(isExecuting =>
                {
                    var spinnerAnimation = (Storyboard) this.Resources["RotateConfirmSpinner"];
                    if (isExecuting) spinnerAnimation.Begin();
                    else spinnerAnimation.Stop();
                })
                .DisposeWith(disposeBag);

            // login error
            ViewModel.LoginErrorMessage
                .Subscribe(errorMessage => validationErrorControl.ErrorText = errorMessage)
                .DisposeWith(disposeBag);

            // forgot password
            // this.forgotPasswordHyperlink.Command =
            //     ReactiveCommand.Create(() => Process.Start("chrome.exe", "https://toggl.com/track/forgot-password?desktop=true"));
            // Disposable.Create(this.forgotPasswordHyperlink, x => x.Command = null)
            //     .DisposeWith(disposeBag);
        }
    }

    public static class ViewExtensions
    {
        public static ICommand ToCommand<T1, T2>(this RxAction<T1, T2> viewAction)
        {
            return ReactiveCommand.CreateFromObservable<T1, T2>(viewAction.ExecuteWithCompletion);
        }

        public static Action<string> TextObserver(this PasswordBox passwordBox, bool ignoreUnchanged = false)
        {
            return password =>
            {
                if (!ignoreUnchanged)
                {
                    passwordBox.Password = password;
                    return;
                }

                if (passwordBox.Password != password)
                    passwordBox.Password = password;
            };
        }
    }
}

