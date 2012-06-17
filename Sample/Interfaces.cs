#region (c) 2012-2012 Lokad - New BSD License 

// Copyright (c) Lokad 2012-2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

namespace Sample
{
    public interface IApplicationService
    {
        void Execute(ICommand cmd);
    }


    public interface IEvent {}

    public interface ICommand {}

    public interface IIdentity {}
}