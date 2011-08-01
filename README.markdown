AzureScaleMe
============

A simple application which allows you to scale Windows Azure instances using user-defined metrics.

Introduction
------------

The ability to quickly scale the number of servers running your application is one of the most compelling features of the Windows Azure platform. However, the ability to do this automatically is not a feature currently available in platform. A number of tools and samples are available demonstrating how this can be done but they are either over-complicated, inflexible or incomplete. AzureScaleMe attempts to overcome this problem with a simple programming model which supports extensibility. This means that if you don't like the way metrics are captured, you can plug your own code in by implementing one method and changing some configuration.

Deployment
----------

AzureScaleMe supports two modes of deployment. 

 - It will run as a stand-alone console application.
 - It can be deployed as a Worker Role on Windows Azure (the tool is designed to run as a single instance only).

How it works
------------

AzureScaleMe has 3 key classes:

### RoleMonitor

Represents a paticular role you wish to monitor. You can have as many RoleMonitors as you like, so you can monitor multiple applications with a single instance of AzureScaleMe. 
A RoleMonitor has one or more IMetricProvider objects. 
A RoleMonitor has one IScalingProvider.

### IMetricProvider

Observes a paticular metric, and provides a recommendation on whether the instance count should be increased, decreased, or maintained. Implementations of this interface will normally expose configuration settings, allowing the user to choose whether a paticular reading is an indication that a scale up/down is required. 

The IMetricProvider interface has a single method:

    public interface IMetricProvider
    {
        int GetMetrics();
    }

Within this method, you should inspect your metric, and return an integer indicating whether a scale up is required (1 or more), a scale down is required (-1 or less) or the instance count should be maintained (0).

Two 'out of the box' metric providers are included. 'PerfCounterMetricProvider' and 'QueueMetricProvider' monitor a given performance counter and the size of an Azure queue respectively.

### IScalingProvider

The IScalingProvider is responsible for adjusting the number of instances of the given role. The interface contains one method, Scale, which is passed the number of instances add/remove from the current configuration. The IScalingProvider is not called if the delta is zero. The method should return a flag to indicate success.

    public interface IScalingProvider
    {
        bool Scale(int delta);
    }

If successful, the time for this change is recorded, and the RoleMonitor will prevent further scaling until the time for 'MinScalingInterval' is reached. If 'MinScalingInterval' is set to '30', another scale will not happen for 30 minutes, regardless of the results returned by the metric providers.

Configuring the default providers
---------------------------------

Within the application .config file, there is a Spring section (http://www.springframework.net/) which allows the user to choose which providers are used for metrics and scaling, and how they are configured. This is also how 3rd party providers can be added.

### QueueMetricProvider

 - __StorageConnectionString__ The connection string to use to locate the queue to be monitored. (i.e. "UseDevelopmentStorage=true")
 - __QueueName__ The name of the queue to be monitored. (i.e. "foo")
 - __MaxValue__ The maximum acceptable length of a queue. A value greater than this will signal a scale up. (i.e. 100)
 - __MinValue__ The minimum acceptable length of a queue. A value smaller than this will signal a scale down. (i.e. 5)
 - __MaxThresholdWait__ The number of minutes for which the queue should be out of the acceptable range before a scale is triggered. (i.e. 2)

