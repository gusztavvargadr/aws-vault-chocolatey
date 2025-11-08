local_mode true
chef_zero.enabled true
cookbook_path [File.expand_path('../cookbooks', __FILE__)]
node_path File.expand_path('../nodes', __FILE__)

# Disable authentication for local mode
authentication false
client_key false
validation_key false
