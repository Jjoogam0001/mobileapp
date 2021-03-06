﻿using System;
using System.Reactive.Linq;
using AuthenticationServices;
using Foundation;
using Toggl.Core.UI.ViewModels;
using Toggl.iOS.Extensions;
using Toggl.iOS.Extensions.Reactive;
using Toggl.iOS.Helper;
using Toggl.Shared;
using Toggl.Shared.Extensions;
using UIKit;

namespace Toggl.iOS.ViewControllers
{
    public partial class LoginViewController : KeyboardAwareViewController<LoginViewModel>
    {
        private ASAuthorizationAppleIdButton appleSignInButton;
        private IDisposable appleSignInButtonDisposable;

        private readonly UIStringAttributes plainTextAttributes = new UIStringAttributes
        {
            ForegroundColor = ColorAssets.Text,
            Font = UIFont.SystemFontOfSize(15, UIFontWeight.Regular)
        };

        public LoginViewController(LoginViewModel vm) : base(vm, nameof(LoginViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            prepareViews();

            var signUpButton = createSignUpButton();
            var closeButton = new UIBarButtonItem(
                UIImage.FromBundle("icClose").ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate),
                UIBarButtonItemStyle.Plain,
                (sender, args) => ViewModel.Close());
            closeButton.TintColor = ColorAssets.IconTint;

            var backButton = new UIBarButtonItem("",
                UIBarButtonItemStyle.Plain,
                (sender, args) => ViewModel.Close());
            backButton.TintColor = ColorAssets.IconTint;

            NavigationItem.RightBarButtonItem = new UIBarButtonItem(signUpButton);
            NavigationItem.LeftBarButtonItem = closeButton;
            NavigationItem.BackBarButtonItem = backButton;

            //E-mail
            ViewModel.Email
                .Select(email => email.ToString())
                .Subscribe(EmailTextField.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            EmailTextField.Rx().Text()
                .Select(Email.From)
                .Subscribe(ViewModel.Email.Accept)
                .DisposedBy(DisposeBag);

            //Password
            ViewModel.Password
                .Select(password => password.ToString().Length > 0)
                .Subscribe(ShowPasswordButton.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            PasswordTextField.Rx().Text()
                .Select(Password.From)
                .Subscribe(ViewModel.Password.Accept)
                .DisposedBy(DisposeBag);

            ViewModel.PasswordVisible
                .Skip(1)
                .Select(CommonFunctions.Invert)
                .Subscribe(PasswordTextField.Rx().SecureTextEntry())
                .DisposedBy(DisposeBag);

            ViewModel.Password
                .Select(password => password.ToString())
                .Subscribe(PasswordTextField.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            //Errors
            ViewModel.EmailErrorMessage
                .Subscribe(EmailErrorLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            ViewModel.PasswordErrorMessage
                .Subscribe(PasswordErrorLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            ViewModel.LoginErrorMessage
                .Subscribe(LoginErrorLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            ViewModel.ShakeEmail
                .Subscribe(EmailTextField.Rx().Shake())
                .DisposedBy(DisposeBag);

            //Actions
            ShowPasswordButton.Rx()
                .BindAction(ViewModel.TogglePasswordVisibility)
                .DisposedBy(DisposeBag);

            signUpButton.Rx()
                .BindAction(ViewModel.SignUp)
                .DisposedBy(DisposeBag);

            LoginButton.Rx()
                .BindAction(ViewModel.Login)
                .DisposedBy(DisposeBag);

            ForgotPasswordButton.Rx()
                .BindAction(ViewModel.ForgotPassword)
                .DisposedBy(DisposeBag);

            //Loading: disabling all interaction
            ViewModel.IsLoading
                .Select(CommonFunctions.Invert)
                .Subscribe(LoginButton.Rx().Enabled())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .Select(CommonFunctions.Invert)
                .Subscribe(signUpButton.Rx().Enabled())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .Select(CommonFunctions.Invert)
                .Subscribe(closeButton.Rx().Enabled())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .Select(CommonFunctions.Invert)
                .Subscribe(ForgotPasswordButton.Rx().Enabled())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .Subscribe(this.Rx().ModalInPresentation())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .Select(CommonFunctions.Invert)
                .Subscribe(ShowPasswordButton.Rx().Enabled())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .Select(CommonFunctions.Invert)
                .Subscribe(EmailTextField.Rx().Enabled())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .Select(CommonFunctions.Invert)
                .Subscribe(PasswordTextField.Rx().Enabled())
                .DisposedBy(DisposeBag);

            //Loading: making everything look disabled
            ViewModel.IsLoading
                .Select(opacityForLoadingState)
                .Subscribe(LogoImageView.Rx().AnimatedAlpha())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .Select(opacityForLoadingState)
                .Subscribe(LoginButton.Rx().AnimatedAlpha())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .Select(opacityForLoadingState)
                .Subscribe(signUpButton.Rx().AnimatedAlpha())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .Select(opacityForLoadingState)
                .Subscribe(WelcomeLabel.Rx().AnimatedAlpha())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .Select(opacityForLoadingState)
                .Subscribe(EmailTextField.Rx().AnimatedAlpha())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .Select(opacityForLoadingState)
                .Subscribe(PasswordTextField.Rx().AnimatedAlpha())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .Select(opacityForLoadingState)
                .Subscribe(ForgotPasswordButton.Rx().AnimatedAlpha())
                .DisposedBy(DisposeBag);

            var animatedLoadingMessage = TextHelpers.AnimatedLoadingMessage();
            ViewModel.IsLoading
                .CombineLatest(animatedLoadingMessage, loginButtonTitle)
                .Subscribe(LoginButton.Rx().Title())
                .DisposedBy(DisposeBag);

            string loginButtonTitle(bool isLoading, string currentLoadingMessage)
                => isLoading
                    ? currentLoadingMessage
                    : Resources.LoginTitle;
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            EmailTextField.BecomeFirstResponder();
        }

        protected override void KeyboardWillShow(object sender, UIKeyboardEventArgs e)
        {
            var keyboardHeight = e.FrameEnd.Height;
            ScrollView.ContentInset = new UIEdgeInsets(0, 0, keyboardHeight, 0);

            var firstResponder = View.GetFirstResponder();
            if (firstResponder != null && ScrollView.Frame.Height - keyboardHeight < ScrollView.ContentSize.Height)
            { 
                var scrollOffset = firstResponder.Frame.Y - (ScrollView.Frame.Height - keyboardHeight) / 2;
                ScrollView.ContentOffset = new CoreGraphics.CGPoint(0, scrollOffset);
            }
        }

        protected override void KeyboardWillHide(object sender, UIKeyboardEventArgs e)
        {
            ScrollView.ContentInset = new UIEdgeInsets(0, 0, 0, 0);
            ScrollView.SetContentOffset(new CoreGraphics.CGPoint(0, 0), true);
        }

        private float opacityForLoadingState(bool isLoading)
            => isLoading ? 0.6f : 1;

        private void prepareViews()
        {
            WelcomeLabel.Text = Resources.LoginWelcomeMessage;
            EmailTextField.Placeholder = Resources.Email;
            PasswordTextField.Placeholder = Resources.Password;
            LoginButton.SetTitle(Resources.LoginTitle, UIControlState.Normal);
            prepareForgotPasswordButton();

            EmailTextField.ShouldReturn += _ =>
            {
                PasswordTextField.BecomeFirstResponder();
                return false;
            };

            PasswordTextField.ShouldReturn += _ =>
            {
                PasswordTextField.ResignFirstResponder();
                ViewModel.Login.Execute();
                return false;
            };

            ShowPasswordButton.SetupShowPasswordButton();
        }

        private void prepareForgotPasswordButton()
        {
            var forgotPasswordTitle = new NSMutableAttributedString(
                    Resources.LoginForgotPassword,
                    underlineStyle: NSUnderlineStyle.Single
                );
            forgotPasswordTitle.AddAttributes(
                plainTextAttributes,
                new NSRange(0, forgotPasswordTitle.Length)
            );
            ForgotPasswordButton.SetAttributedTitle(
                forgotPasswordTitle,
                UIControlState.Normal
            );
        }

        private UIButton createSignUpButton()
        {
            var buttonTitle = new NSMutableAttributedString(Resources.DoNotHaveAnAccountWithQuestionMark);
            buttonTitle.Append(new NSAttributedString(" "));
            buttonTitle.Append(new NSMutableAttributedString(Resources.SignUp, underlineStyle: NSUnderlineStyle.Single));
            buttonTitle.AddAttributes(
                plainTextAttributes,
                new NSRange(0, buttonTitle.Length)
            );

            var button = new UIButton();
            button.SetAttributedTitle(buttonTitle, UIControlState.Normal);
            return button;
        }
    }
}

