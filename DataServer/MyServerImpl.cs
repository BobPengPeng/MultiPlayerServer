using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Grpc.Core;
using DataAndNetwork;

namespace DataAndNetwork
{
    public class MyServerImpl : NetWork.NetWorkBase
    {
        private BodyTransList _bodyTransList = new BodyTransList();
        private Hashtable _broadCastedHash = new Hashtable();

        //client sent action to server, server return a confirm message back
        public override Task<GrpcFeedMsg> TransAction(GrpcAction request, ServerCallContext context)
        {
            GrpcFeedMsg feedMsg = new GrpcFeedMsg();

            if (request.ActionName.Equals("Stop Apply Data"))
            {
                if (_bodyTransList.DataSource.Contains(context.Peer))
                {
                    int count = _bodyTransList.DataSource.IndexOf(context.Peer);
                    _bodyTransList.SentState[count] = false;
                    feedMsg.MsgType = "BodyTrans stop get";
                }
            }

            if (request.ActionName.Equals("Add Player"))
            {
                if (_bodyTransList.DataSource.Contains(context.Peer))
                {
                    feedMsg.MsgType = "Already Exist Player: " + context.Peer;
                }
                else
                {
                    _bodyTransList.DataSource.Add(context.Peer);
                    _bodyTransList.SentState.Add(false);
                    _bodyTransList.TransList.Add(new BodyTrans());

                    feedMsg.MsgType = "Add Success!";
                }
            }

            if (request.ActionName.Equals("Remove Player"))
            {
                if (!_bodyTransList.DataSource.Contains(context.Peer))
                {
                    feedMsg.MsgType = "Already Removed Player: " + context.Peer;
                }
                else
                {
                    int count = _bodyTransList.DataSource.IndexOf(context.Peer);
                    _bodyTransList.DataSource.RemoveAt(count);
                    _bodyTransList.SentState.RemoveAt(count);
                    _bodyTransList.TransList.RemoveAt(count);





                    feedMsg.MsgType = "Remove Success!";
                }
            }

            if (request.ActionName.Equals("Stop BroadCast"))
            {
                if (!_broadCastedHash.ContainsKey(context.Peer))
                {
                    feedMsg.MsgType = "There's no broadcast in: " + context.Peer;
                }
                else
                {
                    _broadCastedHash.Remove(context.Peer);
                    feedMsg.MsgType = "Stop BroadCast in: " + context.Peer;
                }
            }

            if (request.ActionName.Equals("Add Player"))
            {
                foreach (var t in _bodyTransList.DataSource)
                {
                    foreach (var value in _broadCastedHash.Values)
                    {
                        ((List<BroadCastMsg>) value).Add(new BroadCastMsg
                        {
                            MsgType = request.ActionName,
                            Host = t,
                        });
                    }
                }
            }
            else
            {
                foreach (var value in _broadCastedHash.Values)
                {
                    ((List<BroadCastMsg>) value).Add(new BroadCastMsg
                    {
                        MsgType = request.ActionName,
                        Host = context.Peer,
                    });
                }
            }

            return Task.FromResult(feedMsg);
        }

        //client send body data stream to server, server return a confirm message back
        public override async Task<GrpcFeedMsg> ServerGetBodyTrans(IAsyncStreamReader<BodyTrans> requestStream,
            ServerCallContext context)
        {
            GrpcFeedMsg feedMsg = new GrpcFeedMsg();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            while (await requestStream.MoveNext())
            {
                BodyTrans bodyTrans = requestStream.Current;
                if (!_bodyTransList.DataSource.Contains(context.Peer))
                {
                    feedMsg.MsgType = "There's no player: " + context.Peer + ", please add player first and retry";
                }
                else
                {
                    int count = _bodyTransList.DataSource.IndexOf(context.Peer);
                    _bodyTransList.TransList[count] = bodyTrans;
                }
            }

            stopwatch.Stop();
            return feedMsg;
        }


        // public override async tas

        //server send body stream data to client, client send confirm data back
        public override async Task GetBoradCast(GrpcAction request, IServerStreamWriter<BroadCastMsg> responseStream,
            ServerCallContext context)
        {
            Console.WriteLine(context.Peer + " : On BroadCast!");
            _broadCastedHash.Add(context.Peer, new List<BroadCastMsg>());

            List<BroadCastMsg> broadCastMsgList = (List<BroadCastMsg>) _broadCastedHash[context.Peer];
            while (_broadCastedHash.ContainsKey(context.Peer))
            {
                if (broadCastMsgList.Count > 0)
                {
                    await responseStream.WriteAsync(broadCastMsgList[0]);
                    Console.WriteLine(broadCastMsgList[0].Host + "  " + broadCastMsgList[0].MsgType);
                    broadCastMsgList.RemoveAt(0);
                }

                await Task.Delay(10);
            }

            if (broadCastMsgList.Count > 0)
            {
                await responseStream.WriteAsync(broadCastMsgList[0]);
            }
        }

        public override async Task ClientGetTransList(GrpcAction request,
            IServerStreamWriter<BodyTransList> responseStream, ServerCallContext context)
        {
            if (request.ActionName.Equals("Ask Data"))
            {
                if (_bodyTransList.DataSource.Contains(context.Peer))
                {
                    int count = _bodyTransList.DataSource.IndexOf(context.Peer);
                    _bodyTransList.SentState[count] = true;

                    while (_bodyTransList.DataSource.Contains(context.Peer) && _bodyTransList.SentState[count])
                    {
                        await responseStream.WriteAsync(_bodyTransList);
                        await Task.Delay(16);
                    }
                }
            }
        }
    }
}