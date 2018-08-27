using System;
using System.Collections.Generic;
using System.Text;

namespace PagliacciApi
{
    public class DeliveryLocation
    {
        public string Name { get; private set; }
        public string ID { get; private set; }

        public DeliveryLocation(string Name, string ID)
        {
            this.Name = Name;
            this.ID = ID;
        }
    }
}
