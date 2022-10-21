using System;

namespace Contracts.Commands
{

    public class ProcessPayment
    {
        public Guid OrderId { get; set; }
        public Decimal Amount { get; set; }
        public string IdempotencyKey { get; set; }

        public string Currency { get; set; } = "USD";
    }
    /*  -d "amount"=999 \
  -d "currency"="usd" \
  -d "description"="Example charge" \
  -d "source"="tok_visa" \
  -d "metadata[order_id]"=6735
  source.number
REQUIRED
The card number, as a string without any separators.

source.exp_month
REQUIRED
Two-digit number representing the card's expiration month.

source.exp_year
REQUIRED
Two- or four-digit number representing the card's expiration year.

source.cvc
optional
Card security code. Highly recommended to always include this value, but it's required only for accounts based in European countries.

source.currency
optional
CUSTOM CONNECT ONLY
Required when adding a card to an account (not applicable to customers or recipients). The card (which must be a debit card) can be used as a transfer destination for funds in this currency.

source.name
optional
Cardholder's full name.

source.metadata
optional dictionary
A set of key-value pairs that you can attach to a card object. This can be useful for storing additional information about the card in a structured format.

source.default_for_currency
optional
CUSTOM CONNECT ONLY
Applicable only on accounts (not customers or recipients). If you set this to true (or if this is the first external account being added in this currency), this card will become the default external account for its currency.

source.address_line1
optional
Address line 1 (Street address / PO Box / Company name).

source.address_line2
optional
Address line 2 (Apartment / Suite / Unit / Building).

source.address_city
optional
City / District / Suburb / Town / Village.

source.address_state
optional
State / County / Province / Region.

source.address_zip
optional
ZIP or postal code.

source.address_country
optional
Billing address count*/
}