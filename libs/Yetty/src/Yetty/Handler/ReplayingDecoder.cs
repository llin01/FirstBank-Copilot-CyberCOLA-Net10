namespace Com.Cybersoft4u.Yetty.Handler
{
    #region DEPENDENCY

    using System;
    using System.Collections.Generic;

    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Exception;
    using Com.Cybersoft4u.Yetty.Utils;

    #endregion

    public abstract class ReplayingDecoder<T> : DecodeHandler
    {
        private T currentState;

        private ByteBuf data = new ByteBuf(new byte[] { });

        protected ReplayingDecoder(T currentState)
        {
            this.currentState = currentState;
        }

        protected void Checkpoint(T state)
        {
            this.State(state);
        }

        protected T State()
        {
            return this.currentState;
        }

        protected void State(T state)
        {
            this.currentState = state;
        }

        protected void Discard(ByteBuf dataIn)
        {
            dataIn.SkipAll();
        }

        protected override void StartDecode(ChannelHandlerContext ctx, ByteBuf dataIn, List<object> dataOut)
        {
            this.data.WriteBytes(dataIn.Val());
            try
            {
                base.StartDecode(ctx, this.data, dataOut);
            }
            catch (DataNotEnoughException)
            {
                // ignored
            }
            catch (Exception exception)
            {
                ctx.FireExceptionCaught(exception);
            }
            finally
            {
                this.data = new ByteBuf(this.data.Val());
            }
        }
    }
}
