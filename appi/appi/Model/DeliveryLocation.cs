using System;
using System.Collections.Generic;
using System.Text;

namespace PagliacciApi
{
    public class DeliveryLocation
    {
        public string Address { get; private set; }
        internal string ID { get; private set; }
        public bool IsDefault { get; private set; }

        internal DeliveryLocation(string Address, string ID, bool IsDefault)
        {
            this.Address = Address;
            this.ID = ID;
            this.IsDefault = IsDefault;
        }

        public override string ToString()
        {
            return $"Location: '{Address}' (ID = {ID}, IsDefault = {IsDefault}";
        }
    }
}
