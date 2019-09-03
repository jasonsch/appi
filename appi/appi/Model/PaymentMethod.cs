using System;
using System.Collections.Generic;
using System.Text;

namespace PagliacciApi
{
    /*
     * TODO -- Support gift cards.
     */
    public class PaymentMethod
    {
        public enum CardType
        {
            Visa = 0,
            Mastercard = 1,
            AmEx = 2
        }

        public CardType Type { get; private set; }
        public string Name { get; private set; }
        public string ID { get; private set; }

        internal PaymentMethod(string ID, CardType Type, string Name)
        {
            this.ID = ID;
            this.Type = Type;
            this.Name = Name;
        }

        public override string ToString()
        {
            return $"Payment Method: Type: {Type}, ID: {ID}, Name: '{Name}'";
        }
    }
}
