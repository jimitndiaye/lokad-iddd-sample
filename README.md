DRAFT! sample code for IDDD Book 
================================


This is .NET Sample Project to accompany Event Sourcing materials 
from IDDD Book by [Vaughn Vernon](http://vaughnvernon.co/).

Please note, that at the moment of writing this sample project is **draft**. 
Final revision will be delivered simultaneously with the book itself.

### Contents

This project includes sample domain implemented with event sourcing pattern. 
Multiple persistence options are provided:

* Microsoft SQL Server
* MySQL
* File storage
* Windows Azure Blob Storage

### Usage

Open this solution in Visual Studio (ModoDevelop might also work) and
run the project. The project is auto-configured to use file storage by default,
and wipes it on every start.

You should see something like:

```
Create customer-12 named 'Lokad' with Eur
  customer-12r0: Customer Lokad created with Eur
Rename customer-12 to 'Lokad SAS'
  customer-12r1: Customer renamed from 'Lokad' to 'Lokad SAS'
Add 15 EUR - 'Cash'
  customer-12r2: Added 'Cash' 15 EUR | Tx 1 => 15 EUR
Charge 20 EUR - 'Forecasting'
  customer-12r3: Charged 'Forecasting' 20 EUR | Tx 2 => -5 EUR
```

Then, you can dive into the code or try plugging in other types of stores.
Each store requires connection string and will auto-create all required resources
automatically.

### Want More?

If you want more details, here's what you can do next:

* Check out the [Domain Driven Design community](http://dddcommunity.org/)
* Dive into [Lokad.CQRS Sample Project](http://lokad.github.com/lokad-cqrs/) (much more detailed version of this sample)
* Follow [@VaughnVernon](https://twitter.com/#!/VaughnVernon).
* Follow [@abdullin](https://twitter.com/#!/abdullin).

If you have any questions, please feel free to ask contributors.

### Authors and Contributors

* [Vaughn Vernon](http://vaughnvernon.co/), Book author and reviewer
* [Rinat Abdullin](http://abdullin.com), Tech Leader at [Lokad](http://www.lokad.com/), Big Data Analytics for Retail.