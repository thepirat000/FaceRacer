sudo systemctl stop faceracer.service
sudo journalctl -u faceracer.service -n 10 --no-pager

