using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using HSNXT.DSharpPlus.ModernEmbedBuilder;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace All_I_Want_For_Christmas_Bot
{
    public class BotCommands : BaseCommandModule
    {
        public static int step = 0;
        static bool Delivery = false;

        [Command("shotgun"), Description("The only way to relieve the pain of Christmas, albeit only for a while")]
        public async Task Shotgun(CommandContext ctx)
        {
            if (Program.currentChannel == null)
            {
                ModernEmbedBuilder embed = new ModernEmbedBuilder();
                embed.Title = "Whoa there! Trigger happy? She's already dead...";
                embed.ImageUrl = "https://i.imgur.com/SImTbSV.png";
                embed.FooterText = "Don't worry it's safe, you can enjoy Christmas for now.";

                embed.Send(ctx.Channel);

                return;
            }

            Random ran = new Random();

            if (step == 0)
            {
                ModernEmbedBuilder embed = new ModernEmbedBuilder();
                embed.Title = "You found the shotgun";
                embed.ImageUrl = "https://i.imgur.com/iimFldz.png";
                embed.FooterText = "Send \"Shotgun\" again to progress.";

                await Program.StartGunTimer();

                await embed.Send(ctx.Channel);

                step++;
            }
            else if (step == 1)
            {
                int chance = ran.Next(101);

                ModernEmbedBuilder embed = new ModernEmbedBuilder();
                embed.Title = "You grabbed the shotgun";
                embed.ImageUrl = "https://i.imgur.com/hsMCfmX.png";
                embed.FooterText = "You check to see if it has ammo.";

                await embed.Send(ctx.Channel);

                embed = new ModernEmbedBuilder();

                if (chance < 5 && !Delivery)
                {
                    embed.Title = "Darn! No ammo! The amazon drone delivery will take 15 minutes!";
                    embed.ImageUrl = "https://i.imgur.com/vxz9tWy.png";
                    embed.FooterText = "Mariah Carey lives another day, try again in 15 minutes";

                    await embed.Send(ctx.Channel);

                    Delivery = true;

                    step = 99;
                }
                else
                {
                    embed.Title = "Good thing you made sure to stock up on ammo.";
                    embed.ImageUrl = "https://i.imgur.com/9KbBJ24.png";
                    embed.FooterText = "Keep going you can kill her next!";

                    await embed.Send(ctx.Channel);

                    Delivery = false;

                    step++;
                }

                await Program.StartGunTimer();
            }
            else if (step == 2)
            {
                int chance = ran.Next(101);

                ModernEmbedBuilder embed = new ModernEmbedBuilder();

                if (chance < 5)
                {
                    embed.Title = "You take aim and fire! You missed!";
                    embed.ImageUrl = "https://i.imgur.com/5FZXbpI.png";
                    embed.FooterText = "The shotgun got knocked back to the start, but it will take 15 minutes to get it back.";

                    await embed.Send(ctx.Channel);

                    step = 99;

                    await Program.StartGunTimer();
                }
                else
                {
                    embed.Title = "You shot her!!! Everyone is saved from her Christmas cheer...";
                    embed.ImageUrl = "https://i.imgur.com/ujU3LHN.png";
                    embed.FooterText = "for an amount of time between 30 minutes and 2 hours.";

                    await embed.Send(ctx.Channel);

                    step = 99;

                    await Program.StartGunTimer();

                    await Program.killbot();
                }
            }
            else
            {
                long currentTime = 900000 - Program.shotgunStopWatch.ElapsedMilliseconds;

                ModernEmbedBuilder embed = new ModernEmbedBuilder();

                if (((int)((currentTime) / 1000) / 60) == 1)
                {
                    embed.Title = $"Your hands have autism wait {(int)((currentTime) / 1000) / 60} more minute";
                }
                else if (((int)((currentTime) / 1000) / 60) < 1)
                {
                    embed.Title = $"Your hands have autism wait {((currentTime) / 1000) - (((int)((currentTime) / 1000) / 60) * 60)} more seconds";
                }
                else if ((((currentTime) / 1000) / 60) == 1)
                {
                    embed.Title = $"Your hands have autism wait {((currentTime) / 1000) - (((int)((currentTime) / 1000) / 60) * 60)} more second";
                }
                else
                {
                    embed.Title = $"Your hands have autism wait {(int)((currentTime) / 1000) / 60} more minutes";
                }
                embed.ImageUrl = "https://i.imgur.com/zKILb0n.png";
                if (((currentTime) / 1000) - (((int)((currentTime) / 1000) / 60) * 60) < 10)
                {
                    embed.FooterText = $"{(int)((currentTime) / 1000) / 60}:0{((currentTime) / 1000) - (((int)((currentTime) / 1000) / 60) * 60)} Remaining.";
                }
                else
                {
                    embed.FooterText = $"{(int)((currentTime) / 1000) / 60}:{((currentTime) / 1000) - (((int)((currentTime) / 1000) / 60) * 60)} Remaining.";
                }
                

                await embed.Send(ctx.Channel);
            }

            

            //await Program.killbot();
        }
    }
}
