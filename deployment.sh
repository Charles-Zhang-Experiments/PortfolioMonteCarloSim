wget https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb

sudo apt-get update
sudo apt-get install -y apt-transport-https
sudo apt-get update
sudo apt-get install -y dotnet-sdk-3.1
sudo apt-get install -y dotnet-sdk-5.0
sudo apt-get install -y tmux

dotnet --info

# Formal server setup
# sudo mkdir /var/www/
# sudo mkdir /var/www/
# sudo chown -R dries:www-data /var/www/

# Quick setup
sudo mkdir /root/risk
# Send the files over to /root/risk
chmod -R 777 /root/risk

tmux new -s background
cd /root/risk/ # Necessary for correct content root
dotnet /root/risk/PortfolioBuilder.dll --urls https://0.0.0.0:80
# ctrl+b %: Split
# ctrl+b d: Detach
# ctrl+b o: Swap pane
# Scroll: ctrl+b then [ then usual navigation keys (arros and page up/down), ESC to exit.
tmux a -t background

# SSL
sudo snap install core
sudo snap refresh core
sudo snap install --classic certbot
sudo ln -s /snap/bin/certbot /usr/bin/certbot
sudo certbot certonly --standalone
# Running HTTPS requires NGINX for easier certificate management, otherwise configure Kestrel to use HTTPS is hard.