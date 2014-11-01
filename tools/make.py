import sys, re

ARGUMENT_CONVERTERS = {
	str: lambda x: x,
	int: lambda x: int(x),
	bool: lambda x: x.lower() in ['true', 't', 'y', 'yes', '1']
}

def run(build_module, args, cprint):
	try:
		task_names = parse_args(build_module, args)

		dump_cfg(build_module, cprint)

		for task_name in task_names:
			cprint('Executing %s' % task_name, 'Cyan')
			task = getattr(build_module, task_name)
			task()
		cprint('Make Succeeded!', 'Green')
	except:
		cprint('Make Failed!', 'Red')
		raise

def parse_args(build_module, args):
	tasks = []
	for arg in [re.split('\s*=\s*', x, 1) for x in args]:
		if len(arg) == 2:
			arg_type = type(getattr(build_module, arg[0], ''))
			arg_convrter = ARGUMENT_CONVERTERS[arg_type]
			setattr(build_module, arg[0], arg_convrter(arg[1]))
		else:
			add_task(build_module, tasks, arg[0])

	if not tasks:
		add_task(build_module, tasks, 'default')

	return tasks

def add_task(build_module, tasks, task_name):
	task = getattr(build_module, task_name, None)
	if type(task) is list:
		for task_name in task:
			add_task(build_module, tasks, task_name)
	else:
		tasks.append(task_name)

def dump_cfg(build_module, cprint):
	names = [n for n in dir(build_module) if not n.startswith('_') and type(getattr(build_module, n)) in ARGUMENT_CONVERTERS]

	if not names:
		return

	pad = max([len(x) for x in names])

	for name in names:
		cprint(name.rjust(pad) + ': ', 'White', '')
		cprint(str(getattr(build_module, name)))

def ironpython_cprint(message, fg='Gray', end='\n'):
	import System
	System.Console.ForegroundColor = getattr(System.ConsoleColor, fg)
	sys.stdout.write(message)
	sys.stdout.write(end)
	System.Console.ResetColor()

if __name__ == '__main__':
	import os, importlib, traceback

	build_path = os.path.abspath(sys.argv[1])
	build_args = sys.argv[2:]
	build_dir = os.path.dirname(build_path)
	build_file = os.path.basename(build_path)
	build_module_name, build_ext = os.path.splitext(build_file)

	sys.path.insert(0, build_dir)
	build_module = importlib.import_module(build_module_name)

	try:
		run(build_module, build_args, ironpython_cprint)
	except:
		traceback.print_exc()
		sys.exit(1)
