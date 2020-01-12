name 'chocolatey-package'
default_source :supermarket
run_list 'chocolatey-package::default'
cookbook 'chocolatey-package', path: '.'
