using Shouldly;
using System;
using Xunit;

namespace Eventfully.Core.UnitTests
{
    public class MetaDataTests
    {

        public MetaDataTests()
        {

        }


        [Fact]
        public void Should_calculate_expiration()
        {
            var now = DateTime.UtcNow;
            var future = now.AddSeconds(1);
            var past = now.AddSeconds(-1);

            MessageMetaData md = new MessageMetaData() { ExpiresAtUtc = now};
            md.IsExpired(future).ShouldBeTrue();
            md.IsExpired(past).ShouldBeFalse();
            md.IsExpired(now).ShouldBeTrue(); //expirate <=
        }

        [Fact]
        public void Should_default_skip_transient_dispatch_true()
        {
            MessageMetaData md = new MessageMetaData();
            md.SkipTransientDispatch.ShouldBeFalse();

        }

        [Fact]
        public void Should_ignore_null_values_in_constructor()
        {
            MessageMetaData md = new MessageMetaData();
            md.ContainsKey(HeaderType.MessageId.Value).ShouldBeFalse();
            md.ContainsKey(HeaderType.DispatchDelay.Value).ShouldBeFalse();
            md.ContainsKey(HeaderType.CorrelationId.Value).ShouldBeFalse();
            md.ContainsKey(HeaderType.ExpiresAtUtc.Value).ShouldBeFalse();

        }

    }





}