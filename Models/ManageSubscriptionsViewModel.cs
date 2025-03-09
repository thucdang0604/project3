using System.Collections.Generic;

namespace JamesThewProject.Models
{
    public class ManageSubscriptionsViewModel
    {
        public List<Subscription> Subscriptions { get; set; } = new List<Subscription>();
        public List<SubscriptionPlan> Plans { get; set; } = new List<SubscriptionPlan>();
    }
}
