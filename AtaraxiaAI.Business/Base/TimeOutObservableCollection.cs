using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Base
{
    public class TimeOutObservableCollection<T> : ObservableCollection<T>
    {
        /// <summary>
        /// Adds an object to the end of the System.Collections.ObjectModel.Collection`1, then removes the item after it expires.
        /// </summary>
        /// <param name="item">The object to be added to the end of the System.Collections.ObjectModel.Collection`1. The value can be null for reference types.</param>
        /// <param name="expiresIn">Time until the item expires and should be removed from the collection. Default is 5 seconds.</param>
        public void AddTimeOut(T item, TimeSpan? expiresIn = null)
        {
            base.Add(item);

            Task.Run(() =>
            {
                Thread.Sleep(expiresIn ?? new TimeSpan(0, 0, 5));
                this.Remove(item);
            });
        }
    }
}
