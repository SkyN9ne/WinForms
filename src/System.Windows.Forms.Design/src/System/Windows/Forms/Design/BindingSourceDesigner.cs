﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.Design;

namespace System.Windows.Forms.Design;

internal class BindingSourceDesigner : ComponentDesigner
{
    private bool _bindingUpdatedByUser;

    public bool BindingUpdatedByUser
    {
        set
        {
            _bindingUpdatedByUser = value;
        }
    }

    public override void Initialize(IComponent component)
    {
        base.Initialize(component);

        IComponentChangeService componentChangeService = GetService<IComponentChangeService>();
        if (componentChangeService is not null)
        {
            componentChangeService.ComponentChanged += OnComponentChanged;
            componentChangeService.ComponentRemoving += OnComponentRemoving;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            IComponentChangeService componentChangeService = GetService<IComponentChangeService>();
            if (componentChangeService is not null)
            {
                componentChangeService.ComponentChanged -= OnComponentChanged;
                componentChangeService.ComponentRemoving -= OnComponentRemoving;
            }
        }

        base.Dispose(disposing);
    }

    private void OnComponentChanged(object? sender, ComponentChangedEventArgs e)
    {
        if (_bindingUpdatedByUser && e.Component == Component &&
            e.Member is not null && (e.Member.Name == "DataSource" || e.Member.Name == "DataMember"))
        {
            _bindingUpdatedByUser = false;

            DataSourceProviderService dataSourceProviderService = GetService<DataSourceProviderService>();
            dataSourceProviderService?.NotifyDataSourceComponentAdded(Component);
        }
    }

    private void OnComponentRemoving(object? sender, ComponentEventArgs e)
    {
        BindingSource? bingSource = Component as BindingSource;
        if (bingSource is not null && bingSource.DataSource == e.Component)
        {
            IComponentChangeService componentChangeService = GetService<IComponentChangeService>();
            string previousDataMember = bingSource.DataMember;

            PropertyDescriptorCollection propertyDescriptorCollection = TypeDescriptor.GetProperties(bingSource);
            PropertyDescriptor? propertyDescriptor = propertyDescriptorCollection?["DataMember"];

            if (componentChangeService is not null && propertyDescriptor is not null)
            {
                componentChangeService.OnComponentChanging(bingSource, propertyDescriptor);
            }

            bingSource.DataSource = null;

            if (componentChangeService is not null && propertyDescriptor is not null)
            {
                componentChangeService.OnComponentChanged(bingSource, propertyDescriptor, previousDataMember, string.Empty);
            }
        }
    }
}
