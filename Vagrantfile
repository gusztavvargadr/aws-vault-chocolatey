directory = File.dirname(__FILE__)

require "#{directory}/lib/gusztavvargadr/vagrant/samples/environments/vagrant"

VagrantDeployment.defaults_include(
  'machines' => {
    'defaults' => VagrantDockerWindowsMachine.defaults,
  }
)

VagrantDeployment.configure(directory)
