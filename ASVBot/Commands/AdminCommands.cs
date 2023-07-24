﻿using ASVPack.Models;
using DSharpPlus.Entities;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASVPack;
using SavegameToolkit;
using ASVBot.Data;
using System.Reflection.PortableExecutable;
using DSharpPlus.SlashCommands.Attributes;
using ASVBot.Config;

namespace ASVBot.Commands
{
    [SlashCommandGroup("asv-admin", "Admin commands",false)]
    public class AdminCommands: ApplicationCommandModule
    {
        IContentContainer arkPack;
        IDiscordPlayerManager playerManager;
        BotConfig botConfig;
        IResponseDataFormatter dataFormatter;

        public AdminCommands(IContentContainer arkPack, IDiscordPlayerManager playerMan, BotConfig config,IResponseDataFormatter responseFormatter) 
        { 
            this.arkPack = arkPack;
            this.playerManager = playerMan;
            this.botConfig = config;
            this.dataFormatter = responseFormatter;
        }



        [SlashCommand("list-users", "List discord users of ASVBot.")]
        public async Task GetUsers(InteractionContext ctx, [Option("unverified", "Show only unverified users.")]bool onlyUnverified)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            string reportHeader = "User,Ark Id,Ark Name,Max Radius,Location,Stats,Maps";
            List<string> reportLines = new List<string>();

            foreach(var botUser in playerManager.GetPlayers().OrderBy(o => o.DiscordUsername))
            {
                if(onlyUnverified && botUser.IsVerified)
                {
                    //ignore
                }
                else
                {
                    reportLines.Add($"{botUser.DiscordUsername},{botUser.ArkPlayerId},{botUser.ArkCharacterName},{botUser.MaxRadius},{botUser.ResultLocation},{botUser.ResultStats},{botUser.MarkedMaps}");
                }
            }
            
            string responseString = dataFormatter.FormatResponseTable(reportHeader, reportLines);

            var tmpFilename = Path.GetTempFileName();
            File.WriteAllText(tmpFilename, responseString);
            FileStream fileStream = new FileStream(tmpFilename, FileMode.Open, FileAccess.Read);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("").AddFile("asvbot-users.txt", fileStream));



        }

        //asv-player-remove
        //asv-player-deny

        [SlashCommand("verify-user","Verify a user request to link to an ARK character.")]
        public async Task VerifyUser(InteractionContext ctx, [Option("discordUsername","Discord user to verify")]string discordUsername, [Option("userRadius", "Max radius to scan around player location.")]double radius, [Option("showLoc", "Include location data in responses.")]bool showLoc, [Option("showStats", "Show creature statistics in responses.")]bool showStats, [Option("allowMaps", "Allow user to request map images with markers of creature locations.")]bool allowMaps)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            string responseString = $"No discord user link request found matching: {discordUsername}";

            var discordPlayerLink = playerManager.GetPlayers().FirstOrDefault(d => d.DiscordUsername.ToLower() == discordUsername.ToLower());
            if(discordPlayerLink != null )
            {
                discordPlayerLink.IsVerified=true;
                discordPlayerLink.MaxRadius = (float)radius;
                discordPlayerLink.ResultLocation = showLoc;
                discordPlayerLink.ResultStats = showStats;
                discordPlayerLink.MarkedMaps = allowMaps;
                responseString = $"Account link verified: {discordUsername} now linked with {discordPlayerLink.ArkCharacterName} ({discordPlayerLink.ArkPlayerId})";
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(responseString));

        }

        [SlashCommand("remove-user", "Remove user from any pending request and deny lists.")]
        public async Task RemoveUser(InteractionContext ctx, [Option("discordUsername", "Discord user to verify")] string discordUsername)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            string responseString = $"Account link request removed: {discordUsername})";

            playerManager.RemovePlayer(discordUsername);            

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(responseString));

        }


        [SlashCommand("deny-user", "Reject any user request to link to an ARK character and deny and future requests.")]
        public async Task DenyUser(InteractionContext ctx, [Option("discordUsername", "Discord user to deny")] string discordUsername)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            string responseString = $"Account link denied: {discordUsername})";
            playerManager.DenyPlayer(discordUsername);
            
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(responseString));

        }


        [SlashCommand("save", "Commit any data changes since last save.")]
        public async Task SavePlayers(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            playerManager.Save();
            botConfig.Save();
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Bot data saved."));
        }

        [SlashCommand("load", "Load ARK save game data.")]
        public async Task Load(InteractionContext ctx, [Option("arkSaveFile", ".ark filename to load.")]string arkFilename, [Option("arkClusterFolder", "Cluster folder (optional)")]string clusterFolder="")
        {

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            string responseString = string.Empty;

            arkPack = new ContentContainer();
            arkPack.LoadSaveGame(arkFilename, string.Empty, clusterFolder);
            botConfig.ArkSaveFile = arkFilename;
            botConfig.ArkClusterFolder = clusterFolder;
            botConfig.Save();
            
            responseString = $"Map loaded: {arkPack.LoadedMap.MapName} ({arkPack.GameSaveTime.ToString()})";

            //Some time consuming task like a database call or a complex operation
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(responseString));
        }

        [SlashCommand("reload", "Re-load the save game data if timestamp has changed.")]
        public async Task Reload(InteractionContext ctx)
        {

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            string responseString = string.Empty;

            if (arkPack.Reload())
            {
                responseString = $"Map reloaded: {arkPack.LoadedMap.MapName} ({arkPack.GameSaveTime.ToString()})";
            }
            else
            {
                responseString = $"Map already up to date: {arkPack.LoadedMap.MapName} ({arkPack.GameSaveTime.ToString()})";
            }           

            //Some time consuming task like a database call or a complex operation
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(responseString));
        }
    }
}
