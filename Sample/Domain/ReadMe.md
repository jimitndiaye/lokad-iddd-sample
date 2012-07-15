> Rinat Abdullin, 2012-07-15

This folder contains domain of this IDDD Sample. In DDD+ES projects this
usually is the most important part of the code. This is where core business
concepts are captured!

Everything else (like infrastructure and storage implementations) is completely
disposable. Normally, you should be able to take your domain code and swap
infrastructure detail with relative use (e.g. switching from local dev machine
to Windows Azure cloud).

At least, this is how we develop things at Lokad.