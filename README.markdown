AzureScaleMe
============

A simple application which allows you to scale Windows Azure instances using user-defined metrics.

Introduction
------------

The ability to quickly scale the number of servers running your application is one of the most compelling features of the Windows Azure platform. However, the ability to do this automatically is not a feature currently available. A number of tools and samples demonstrate how this can be done but they are either very complicated, inflexible or incomplete. AzureScaleMe attempts to overcome this problem with a simple programming model which supports extensibility. This means that if you don't like the way metrics are captured, you can plug your own code in by implementing one method and changing some configuration.

Deployment
----------

AzureScaleMe supports two modes of deployment. 

 - It will run as a stand-alone console application.
 - It can be deployed as a Worker Role on Windows Azure (the application is designed to run as a single instance only).

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

This metric provider will monitor a message queue, and report whether a scale up/down is required based on the treshold values set.

 - __StorageConnectionString__ The connection string to use to locate the queue to be monitored. (i.e. "UseDevelopmentStorage=true")
 - __QueueName__ The name of the queue to be monitored. (i.e. "foo")
 - __MaxValue__ The maximum acceptable length of a queue. A value greater than this will signal a scale up. (i.e. 100)
 - __MinValue__ The minimum acceptable length of a queue. A value smaller than this will signal a scale down. (i.e. 5)
 - __MaxThresholdWait__ The number of minutes for which the queue should be out of the acceptable range before a scale is triggered. (i.e. 2)

 > In this example, if a queue length is greater than 100 for 2 minutes, a scale up will be triggered. If the queue length is less than 5 for 2 minutes, and scale down will be triggered.

The configuration file looks like this:

	<object type="Two10.AzureScaleMe.MetricProviders.QueueMetricProvider">
		<property name="StorageConnectionString" value="UseDevelopmentStorage=true" />
		<property name="QueueName" value="foo" />
		<property name="MaxValue" value="100" />
		<property name="MinValue" value="5" />
		<property name="MaxThresholdWait" value="2" />
	</object>

### PerfCounterMetricProvider

This metric provider will monitor a performance counter, and report whether a scale up/down is required based on the threshold values set.

 - __StorageConnectionString__ The connection string to use to store the performance counters. (i.e. "UseDevelopmentStorage=true")
 - __Counter__ The name of the perf counter to be monitored (i.e. "\Processor(_Total)\% Processor Time")
 - __MaxValue__  The maximum acceptable value for the counter. A value greater than this will signal a scale up. (i.e. 80)
 - __MinValue__ The minimum acceptable value for the counter. A value smaller than this will signal a scale down. (i.e. 5)
 - __MaxThresholdWait__  The number of minutes for which the queue should be out of the acceptable range before a scale is triggered. (i.e. 20)
 - __SampleRate__ The frequency (in seconds) which the counter will be sampled. (i.e. 5)
 - __SamplePeriod__ The number of minutes over which to average the counter value (i.e. 10)
 - __SubscriptionId__ The SubscriptionId of the subscription which holds the role to be monitored.
 - __ServiceName__ The name of the service which holds the role to be monitored.
 - __RoleName__ The name of the role to be monitored.
 - __CertificateThumbprint__ The thumbprint of the certificate to use for setting up the performance counters.
 - __ConfigureCounters__ A boolean value, used to control whether counters should be automatically configured or not. (i.e. True)
 - __CounterTableName__ The table name to store and retrieve the counter information (at the moment, Azure will always call this 'WADPerformanceCountersTable').

 > In this example, a scale up will be triggerd after the CPU time averaged over 10 minutes exceeds 80% for 20 minutes for all roles instances. A scale down will be triggered when if the CPU time averaged over 10 minutes is less than 5% for 20 minutes for all role isntances.

The configuration file looks like this:

	<object type="Two10.AzureScaleMe.MetricProviders.PerfCounterMetricProvider">
		<property name="StorageConnectionString" value="UseDevelopmentStorage=true" />
		<property name="Counter" value="\Processor(_Total)\% Processor Time" />
		<property name="MaxValue" value="80" />
		<property name="MinValue" value="5" />
		<property name="MaxThresholdWait" value="20" />
		<property name="SampleRate" value="5" />
		<property name="SamplePeriod" value="10" />
		<property name="SubscriptionId" value="82f8e1cd-ee8d-414e-b6a5-be7b18a1fa89" />
		<property name="ServiceName" value="ServiceName" />
		<property name="RoleName" value="RoleName" />
		<property name="CertificateThumbprint" value="B929E1E212A92B9CA67F7445F9CB8BF09EC5231E" />
		<property name="ConfigureCounters" value="True" />
		<property name="CounterTableName" value="WADPerformanceCountersTable" />
	</object>

If performance counters are already configured, the metric provider can be pointed at the existing table capturing the data. If not, the provider can create the tables, and configure the roles accordingly.

### IncrementalScaler

This scaling provider will modify the instance count by 1, within the bounds specified.

 - __MinInstances__ The minimum number of acceptable instances. (i.e. 1)
 - __MaxInstances__ The maximum number of acceptable instances. (i.e. 10)
 - __SubscriptionId__ The SubscriptionId of the subscription which holds the role to be scaled.
 - __ServiceName__ The name of the service which holds the role to be scaled. 
 - __RoleName__ The name of the role to be scaled.
 - __CertificateThumbprint__ The thumbprint of the certificate to use for updating the role.

The configuration file looks like this:

    <object type="Two10.AzureScaleMe.ScalingProviders.IncrementalScaler">
        <property name="MinInstances" value="1" />
        <property name="MaxInstances" value="10" />
        <property name="SubscriptionId" value="82f8e1cd-ee8d-414e-b6a5-be7b18a1fa89" />
        <property name="ServiceName" value="ServiceName" />
        <property name="RoleName" value="RoleName" />
        <property name="CertificateThumbprint" value="B929E1E212A92B9CA67F7445F9CB8BF09EC5231E" />
    </object>

RoleMonitor
-----------

The RoleMonitor controls how the metrics are totaled, to provide a figure for the scaling provider. It is also responsible for introducing an interval between scaling attempts, as a role may take time to provision, and it's affect on the metrics may not be immediate. Therefore it is necessary to introduce a period after a scaling, for which no action should be taken. Metrics over this period are still captured, but the result is not forwarded to the scaling provider.

There are three options for combining together the results of the metric providers. Min, Max orSum. 

Min will report the lowset provided metric result (optimistic). 
Max will report the higest value (pessimistic). 
Sum will add together all results.

 > For example, if one metric is returning a scale up (+1) and another is returning scale down (-1). The min function will return -1, the max function will return +1, and the sum function will return 0. This setting should be carefully chosen if more than one metric is employed. If there is only one metric, it's irrelevant.

The settings of a RoleMonitor are:

 - __Name__ An arbitrary name.
 - __MinScalingInterval__ The minimum number of minutes between two scaling attempts. (i.e. 30)
 - __CompositionStrategy__ The mechanism to use to compose all metric responses into a scaling attempt. (i.e. Sum)

 The configuration file looks like this:

    <object type="Two10.AzureScaleMe.RoleMonitor">
        <property name="Name" value="AzureTestProject"/>
        <property name="MinScalingInterval" value="30"/>
        <property name="CompositionStrategy" value="Sum"/>
        ...

Certificates
------------

X509 Certificates are required to configure the performance counters, and to adjust the instance count of a role. The certificate must be installed in the local certificate store, and added to the management certificates in the Azure Portal. The certificate does not need to signed by a root authority, and can be created automatically by Visual Studio.

Certificates can be added to the Worker project (ensure thay are copied to the output directory), and they should be added to the 'Certificate' section of the config file. The certificate should have the private key exported (this creates a .pfx file), and that should be set as the value of 'Item2'.

	<object id="Certificates" type="System.Collections.Generic.List&lt;System.Tuple&lt;string, string&gt;&gt;" singleton="true">
		<constructor-arg name="collection">
			<list element-type="System.Tuple&lt;string, string&gt;">

				<object type="System.Tuple&lt;string, string&gt;">
					<constructor-arg name="Item1" value="certificate.pfx" />
					<constructor-arg name="Item2" value="Password" />
				</object>

			</list>
		</constructor-arg>
	</object>