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

Start by [downloading sources](https://github.com/Lokad/lokad-iddd-sample/zipball/master) (ZIP, Visual Studio) 
or by cloning a [git repository](https://github.com/Lokad/lokad-iddd-sample):

```
$ git clone git://github.com/Lokad/lokad-iddd-sample.git
```


Then, open solution in Visual Studio (ModoDevelop might also work) and
run the project. The project is auto-configured to use file storage by default,
and cleans it on every start.

You should see something like:

```
Command: Create customer-12 named 'Lokad' with Eur
  customer-12 r0 Event: Customer Lokad created with Eur
  customer-12 r0 Event: Added 'Welcome bonus' 15 EUR | Tx 1 => 15 EUR
  Completed in 145 ms
Command: Rename customer-12 to 'Lokad SAS'
  customer-12 r1 Event: Customer renamed from 'Lokad' to 'Lokad SAS'
  Completed in 27 ms
Command: Charge 20 EUR - 'Forecasting'
  customer-12 r2 Event: Charged 'Forecasting' 20 EUR | Tx 2 => -5 EUR
  Completed in 16 ms
```

Then, you can dive into the code by starting with `Domain` folder in this solution.

You can also try plugging in other types of stores. Each store requires connection string 
and will auto-create all required resources automatically.

### Want More?

If you want more details, here's what you can do next:

* Check out the [Domain Driven Design community](http://dddcommunity.org/)
* Dive into [Lokad.CQRS Sample Project](http://lokad.github.com/lokad-cqrs/) (much more detailed and practical version of this sample)
* Check out [CQRS/DDD Pocket Guide](http://cqrsguide.com/case-studies) and [case studies](http://cqrsguide.com/case-studies)
* Follow [@VaughnVernon](https://twitter.com/#!/VaughnVernon).
* Follow [@abdullin](https://twitter.com/#!/abdullin).

If you have any questions, please feel free to ask contributors.

### Authors and Contributors

* [Vaughn Vernon](http://vaughnvernon.co/), Book author and reviewer
* [Rinat Abdullin](http://abdullin.com), A+ES text and sample project. Tech Leader at [Lokad](http://www.lokad.com/), Big Data Analytics for Retail.