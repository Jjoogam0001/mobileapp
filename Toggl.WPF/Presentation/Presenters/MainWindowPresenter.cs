using System;
using System.Threading.Tasks;
using Toggl.Core.UI.Navigation;
using Toggl.Core.UI.ViewModels;
using Toggl.Core.UI.Views;
using Toggl.WPF.Presentation;

namespace Toggl.WPF
{
    public sealed class MainWindowPresenter : IPresenter
    {
        private readonly MainWindow window;

        public MainWindowPresenter(MainWindow window)
        {
            this.window = window;
        }

        public bool CanPresent<TInput, TOutput>(ViewModel<TInput, TOutput> viewModel)
            => true;

        public bool ChangePresentation(IPresentationChange presentationChange)
        {
            throw new NotImplementedException();
        }

        public async Task Present<TInput, TOutput>(ViewModel<TInput, TOutput> viewModel, IView sourceView)
        {
            switch(viewModel)
            {
                case LoginViewModel loginViewModel:
                    var loginView = new LoginView(loginViewModel);
                    window.SetMainView(loginView);
                    break;
            }
        }
    }
}
