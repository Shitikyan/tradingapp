using System.Windows;

namespace TradeApp.Infrastructure.Behavior
{
    public interface IBehavior
    {
        void OnEnabled(DependencyObject dependencyObject);
        void OnDisabling(DependencyObject dependencyObject);
    }
}
