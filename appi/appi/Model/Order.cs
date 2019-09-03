using System;
using System.Collections.Generic;
using System.Text;

namespace PagliacciApi
{
    public class Order
    {
        private ApiClient Client;
        public DeliveryLocation Address { get; set; }
        public PaymentMethod Method { get; set; }

        /*
         * Will receive the default/last-used address as Address (which can be overriden by the user).
         */
        internal Order(ApiClient Client, DeliveryLocation Address)
        {
            this.Client = Client;
            this.Address = Address;
        }

        public bool Submit()
        {
            return Client.SubmitOrder(this);
        }
    }
}