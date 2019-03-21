using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TradeApp.Infrastructure.Behavior
{
    public interface IBehavior
    {
        void OnEnabled(DependencyObject dependencyObject);
        void OnDisabling(DependencyObject dependencyObject);
    }
}
